using FluentAssertions;
using Gateway.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gateway.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static CorsPolicy? GetDefaultCorsPolicy(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddCorsPolicy(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<CorsOptions>>()
            .Value.GetPolicy(ServiceCollectionExtensions.DefaultCorsPolicy);
    }

    [Fact]
    public void AddCorsPolicy_WithNoConfiguredOrigins_AllowsAnyOrigin()
    {
        var policy = GetDefaultCorsPolicy(BuildConfiguration([]));

        policy.Should().NotBeNull();
        policy!.AllowAnyOrigin.Should().BeTrue();
    }

    [Fact]
    public void AddCorsPolicy_WithConfiguredOrigins_RestrictsToThemAndAllowsCredentials()
    {
        var policy = GetDefaultCorsPolicy(BuildConfiguration(new()
        {
            ["Cors:AllowedOrigins:0"] = "https://localhost:3000"
        }));

        policy.Should().NotBeNull();
        policy!.AllowAnyOrigin.Should().BeFalse();
        policy.Origins.Should().ContainSingle().Which.Should().Be("https://localhost:3000");
        policy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddGatewayAuthentication_WithoutJwtKey_Throws()
    {
        var act = () => new ServiceCollection()
            .AddGatewayAuthentication(BuildConfiguration([]));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Key*");
    }
}
