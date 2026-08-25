using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Service.Controllers;

namespace Service.Tests.Controllers;

public class ResilienceControllerTests
{
    private static ResilienceController CreateController() => new(NullLogger<ResilienceController>.Instance);

    [Fact]
    public async Task GetWithTimeout_CompletesWithinTimeout_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.GetWithTimeout(delaySeconds: 0);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Success: data from dependency", okResult.Value);
    }

    [Fact]
    public async Task GetWithTimeout_ExceedsTimeout_Returns504()
    {
        var controller = CreateController();

        // The pipeline's timeout is 2s; a 5s delay forces Polly to cancel the call.
        var result = await controller.GetWithTimeout(delaySeconds: 5);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetWithCircuitBreaker_OpensAfterRepeatedFailures_ThenShortCircuits()
    {
        var controller = CreateController();

        // MinimumThroughput=4 and FailureRatio=0.5: four straight failures trips the breaker
        // open. While it's closed, the dependency actually runs, so the raw exception
        // propagates rather than being caught by the controller's BrokenCircuitException handler.
        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.GetWithCircuitBreaker(fail: true));
        }

        // Circuit is now open: the call is short-circuited without invoking the dependency.
        var result = await controller.GetWithCircuitBreaker(fail: false);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusResult.StatusCode);
    }
}
