namespace Gateway.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseGatewayPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.DefaultCorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
