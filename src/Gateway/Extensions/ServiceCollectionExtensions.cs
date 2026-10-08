using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    public const string DefaultCorsPolicy = "DefaultCorsPolicy";

    /// <summary>Policy name referenced by routes in appsettings ("AuthorizationPolicy").</summary>
    public const string AuthenticatedPolicy = "authenticated";

    /// <summary>
    /// Routes/clusters come from the "ReverseProxy" config section. The Gateway only routes — JWT/API-key
    /// validation stays in Service, so the Authorization / X-Api-Key headers are forwarded untouched.
    /// To split Service into several services later, add clusters/routes in appsettings; no code changes.
    /// </summary>
    public static IServiceCollection AddYarpGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

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

    /// <summary>
    /// Per-client-IP fixed window at the edge, applied to proxied routes via the route's "RateLimiterPolicy".
    /// In-memory per gateway instance; Service still enforces its own (Redis-backed) limit behind it.
    /// </summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var permitLimit = section.GetValue("PermitLimit", 100);
        var windowSeconds = section.GetValue("WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("perClient", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// Validates JWTs at the edge using the same issuer/audience/key as Service, so bad tokens never reach it.
    /// Service still validates the token again (defense in depth) and owns role checks.
    /// A request carrying the API-key header is let through without checking the key — that secret
    /// lives in Service, which validates it.
    /// </summary>
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key configuration is required.");
        var apiKeyHeaderName = configuration["ApiKey:HeaderName"] ?? "X-Api-Key";

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

                // SignalR's WebSocket/SSE transports can't set an Authorization header, so the JS
                // client puts the JWT in the query string — only honor that for the hub path.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthenticatedPolicy, policy => policy.RequireAssertion(context =>
                context.User.Identity?.IsAuthenticated == true ||
                (context.Resource is HttpContext http && http.Request.Headers.ContainsKey(apiKeyHeaderName))));

        return services;
    }
}
