using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.ML;
using Microsoft.IdentityModel.Tokens;
using RedisRateLimiting;
using Service;
using Service.ErrorHandling;
using Service.Services;
using Service.Tools;
using StackExchange.Redis;

namespace Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcp(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<CustomerTools>();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Scans this assembly and auto-registers every class that has exactly one implemented
    /// interface matching its own name by convention (e.g. <c>PersonRepository</c> -> <c>IPersonRepository</c>).
    /// Types that require a factory (e.g. constructor arguments not resolvable from DI) are excluded
    /// and remain registered explicitly where they're configured.
    /// </summary>
    public static IServiceCollection AddScannedServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(classes => classes
                .Where(type => type != typeof(BackgroundTaskQueue)), publicOnly: true)
            .AsMatchingInterface()
            .WithScopedLifetime());

        return services;
    }

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis configuration is required.");

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        services.AddHealthChecks().AddRedis(redisConnectionString, name: "redis");

        // Pub/sub for cross-service notifications, backed by the same Redis instance as the cache.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddHostedService<PersonNotificationSubscriberService>();

        return services;
    }

    public static IServiceCollection AddBackgroundProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));
        services.AddHttpClient();
        services.AddHostedService<AppBackgroundService>();

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // https://github.com/cristipufu/aspnetcore-redis-rate-limiting
        // Counters live in Redis (not process memory) so the limit is enforced across all service instances.
        var rateLimitingSection = configuration.GetSection("RateLimiting");
        var permitLimit = rateLimitingSection.GetValue("PermitLimit", 100);
        var windowSeconds = rateLimitingSection.GetValue("WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = new ProblemDetails
                {
                    // Type = "https://tools.ietf.org/html/rfc6585#section-4"
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests.",
                };

                context.HttpContext.Response.StatusCode = problemDetails.Status.Value;
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };
        });

        // Deferred via IOptions so it resolves the IConnectionMultiplexer already registered by AddRedis,
        // instead of building a second service provider (and a second Redis connection) here.
        services.AddOptions<RateLimiterOptions>().Configure<IConnectionMultiplexer>((options, connectionMultiplexer) =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RedisRateLimitPartition.GetFixedWindowRateLimiter(partitionKey, _ => new RedisFixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    ConnectionMultiplexerFactory = () => connectionMultiplexer
                });
            });
        });

        return services;
    }

    public const string DefaultCorsPolicy = "DefaultCorsPolicy";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicy, policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins).AllowCredentials();
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;   // no version in URL => v1.0
            options.ReportApiVersions = true;                     // adds api-supported-versions / api-deprecated-versions response headers
        })
        .AddMvc()          // wires versioning into the MVC/controller pipeline
        .AddApiExplorer(options =>
        {
            // Calling "AddApiExplorer" is required for OpenAPI versioning to work correctly.
            // Without this, the generated OpenAPI documents will not be versioned.

            // GroupNameFormat specifies the format of the API version.
            // Without this, versioning will use the literal group names. In our case, that would be 1.0.
            // For compatibility with the "default" /openapi/v1.json behavior from Microsoft.AspNetCore.OpenApi, we use v'VVV' so we can retrieve it using v1.json.
            // See https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Format#custom-api-version-format-strings for more information about formatting API versions.
            options.GroupNameFormat = "'v'VVV";        // v1, v2, v1.1 ...
            options.SubstituteApiVersionInUrl = true;  // replaces {version} token in generated docs
        })
        // Asp.Versioning.OpenApi's variant — replaces the standalone Microsoft.AspNetCore.OpenApi
        // AddOpenApi() and makes the generated documents version-aware. Call it once here, not per version.
        .AddOpenApi();

        return services;
    }

    public static IServiceCollection AddMachineLearning(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var mlModelsDir = Path.Combine(environment.ContentRootPath, "MLModels");
        var trainingDataPath = Path.Combine(mlModelsDir, "people.csv");
        var modelPath = Path.Combine(mlModelsDir, "model.zip");

        if (!File.Exists(modelPath))
        {
            Directory.CreateDirectory(mlModelsDir);
            MLModels.ModelBuilder.TrainAndSaveModel(trainingDataPath, modelPath);
        }

        services.AddPredictionEnginePool<PersonData, PersonPrediction>().FromFile(modelName: "PersonSalaryModel", filePath: modelPath, watchForChanges: true);

        return services;
    }
}
