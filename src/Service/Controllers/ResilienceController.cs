using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Service.Controllers;

[Authorize]
[ApiController, Route("[controller]")]
public class ResilienceController(ILogger<ResilienceController> logger) : ControllerBase
{
    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>()
        })
        .Build();

    [HttpGet]
    public async Task<ActionResult<string>> Get()
    {
        var attempt = 0;

        var result = await Pipeline.ExecuteAsync(async token =>
        {
            attempt++;
            logger.LogInformation("Attempt {Attempt} to call flaky dependency", attempt);
            return await CallFlakyDependencyAsync(attempt, token);
        });

        return Ok($"Succeeded on attempt {attempt}: {result}");
    }

    // Simulates a dependency that fails the first two times, then succeeds.
    private static Task<string> CallFlakyDependencyAsync(int attempt, CancellationToken token)
    {
        if (attempt < 3)
        {
            throw new InvalidOperationException($"Simulated transient failure on attempt {attempt}");
        }

        return Task.FromResult("data from dependency");
    }

    private static readonly ResiliencePipeline CircuitBreakerPipeline = new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 4,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>()
        })
        .Build();

    // Call with ?fail=true repeatedly to trip the breaker open, then ?fail=false while
    // it's open to see calls short-circuited (503) without the dependency being invoked.
    // After BreakDuration elapses, the breaker moves to half-open and lets one call through.
    [HttpGet("circuit-breaker")]
    public async Task<ActionResult<string>> GetWithCircuitBreaker([FromQuery] bool fail = false)
    {
        try
        {
            var result = await CircuitBreakerPipeline.ExecuteAsync(async token =>
            {
                logger.LogInformation("Calling dependency (fail={Fail})", fail);
                return await CallUnreliableDependencyAsync(fail, token);
            });

            return Ok($"Success: {result}");
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuit is open, short-circuiting call");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Circuit breaker is open - call rejected without invoking the dependency.");
        }
    }

    private static Task<string> CallUnreliableDependencyAsync(bool fail, CancellationToken token)
    {
        if (fail)
        {
            throw new InvalidOperationException("Simulated dependency failure");
        }

        return Task.FromResult("data from dependency");
    }

    private static readonly ResiliencePipeline TimeoutPipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromSeconds(2))
        .Build();

    // Call with ?delaySeconds=1 to complete within the 2s timeout, or ?delaySeconds=5
    // to see Polly cancel the call and throw TimeoutRejectedException.
    [HttpGet("timeout")]
    public async Task<ActionResult<string>> GetWithTimeout([FromQuery] int delaySeconds = 5)
    {
        try
        {
            var result = await TimeoutPipeline.ExecuteAsync(async token =>
            {
                logger.LogInformation("Calling dependency that takes {DelaySeconds}s", delaySeconds);
                return await CallSlowDependencyAsync(delaySeconds, token);
            });

            return Ok($"Success: {result}");
        }
        catch (TimeoutRejectedException)
        {
            logger.LogWarning("Call timed out after {Timeout}", TimeSpan.FromSeconds(2));
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Call was cancelled - dependency did not respond within the configured timeout.");
        }
    }

    private static async Task<string> CallSlowDependencyAsync(int delaySeconds, CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
        return "data from dependency";
    }
}
