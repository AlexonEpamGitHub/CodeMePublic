using Wolverine.ErrorHandling;
using FluentValidation;

namespace Web.Api.Wolverine.Middleware;

/// <summary>
/// Custom exception policies for Wolverine message handling
/// </summary>
public static class WolverineExceptionPolicies
{
    public static void ConfigureExceptionPolicies(this WolverineOptions opts)
    {
        // Handle validation exceptions
        opts.Policies.OnException<ValidationException>()
            .RetryTimes(0); // Don't retry validation failures
        
        // Handle transient errors with retries
        opts.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
        
        // Handle general exceptions
        opts.Policies.OnException<Exception>()
            .RetryTimes(3)
            .Then
            .MoveToErrorQueue();
    }
}
