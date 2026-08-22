using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.Extensions.ML;
using Service.Tools;

namespace Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddMcpServer()
            .WithHttpTransport(op =>
            {
                op.Stateless = true;
            }).WithTools<CustomerTools>();
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<Data.AppDbContext>(options => options.UseNpgsql(connectionString));
        // builder.Services.AddScoped<IPersonRepository, PersonRepository>();
        builder.Services.AddTransient<IPersonRepository, PersonRepository>();
        
        // ADD THE REQUIRED SERVICES HERE
        builder.Services.AddHealthChecks();
        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // --- JWT bearer authentication ---
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

        builder.Services.AddAuthorization();
        // --- end JWT bearer authentication ---

        // --- ML.NET model training + registration ---
        var mlModelsDir = Path.Combine(builder.Environment.ContentRootPath, "MLModels");
        var trainingDataPath = Path.Combine(mlModelsDir, "people.csv");
        var modelPath = Path.Combine(mlModelsDir, "model.zip");

        if (!File.Exists(modelPath))
        {
            Directory.CreateDirectory(mlModelsDir);
            MLModels.ModelBuilder.TrainAndSaveModel(trainingDataPath, modelPath);
        }

        builder.Services.AddPredictionEnginePool<PersonData, PersonPrediction>()
            .FromFile(modelName: "PersonSalaryModel", filePath: modelPath, watchForChanges: true);
        // --- end ML.NET setup ---
        
        var app = builder.Build();

        try
        {
            Log.Information("Starting web host");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "My Service API";
                    options.Theme = ScalarTheme.Purple;
                    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.MapMcp("/mcp");
            
            // /health (simple)
            app.MapHealthChecks("/health");

            // /health/details (JSON with entries)
            app.MapHealthChecks("/health/details", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration.TotalMilliseconds,
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration.TotalMilliseconds,
                            description = e.Value.Description,
                            data = e.Value.Data
                        })
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
                }
            });

            app.MapGet("/widgets/{id}", (int id) => Results.Ok())
                .WithName("GetWidget")
                .WithSummary("Get a widget by id")
                .WithDescription("Returns a single widget or 404 if not found.");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
