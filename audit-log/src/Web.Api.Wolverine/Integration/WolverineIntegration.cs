using Application.Abstractions.Messaging;
using Wolverine;

namespace Web.Api.Wolverine.Integration;

/// <summary>
/// Bridge between Wolverine's IMessageBus and Application layer's ICommandHandler/IQueryHandler
/// This allows seamless integration with existing CQRS infrastructure
/// </summary>
public class WolverineCommandDispatcher
{
    private readonly IMessageBus _bus;

    public WolverineCommandDispatcher(IMessageBus bus)
    {
        _bus = bus;
    }

    /// <summary>
    /// Execute command through Wolverine message bus
    /// </summary>
    public async Task<SharedKernel.Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return await _bus.InvokeAsync<SharedKernel.Result>(command, cancellationToken);
    }

    /// <summary>
    /// Execute command with result through Wolverine message bus
    /// </summary>
    public async Task<SharedKernel.Result<TResult>> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        return await _bus.InvokeAsync<SharedKernel.Result<TResult>>(command, cancellationToken);
    }
}

/// <summary>
/// Bridge for query execution
/// </summary>
public class WolverineQueryDispatcher
{
    private readonly IMessageBus _bus;

    public WolverineQueryDispatcher(IMessageBus bus)
    {
        _bus = bus;
    }

    /// <summary>
    /// Execute query through Wolverine message bus
    /// </summary>
    public async Task<SharedKernel.Result<TResult>> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        return await _bus.InvokeAsync<SharedKernel.Result<TResult>>(query, cancellationToken);
    }
}

/// <summary>
/// Generic Wolverine handler that automatically routes messages to registered ICommandHandler implementations
/// This eliminates the need to create individual Wolverine handlers for each command
/// </summary>
public class WolverineCommandHandler<TCommand> where TCommand : ICommand
{
    public static async Task<SharedKernel.Result> Handle(TCommand command, ICommandHandler<TCommand> handler)
    {
        return await handler.Handle(command, CancellationToken.None);
    }
}

/// <summary>
/// Generic Wolverine handler for commands with results
/// </summary>
public class WolverineCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public static async Task<SharedKernel.Result<TResult>> Handle(TCommand command, ICommandHandler<TCommand, TResult> handler)
    {
        return await handler.Handle(command, CancellationToken.None);
    }
}

/// <summary>
/// Generic Wolverine handler for queries
/// </summary>
public class WolverineQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    public static async Task<SharedKernel.Result<TResult>> Handle(TQuery query, IQueryHandler<TQuery, TResult> handler)
    {
        return await handler.Handle(query, CancellationToken.None);
    }
}

/// <summary>
/// Extension methods for registering Wolverine-Application integration
/// </summary>
public static class WolverineIntegrationExtensions
{
    /// <summary>
    /// Register Wolverine integration services
    /// </summary>
    public static IServiceCollection AddWolverineIntegration(this IServiceCollection services)
    {
        services.AddScoped<WolverineCommandDispatcher>();
        services.AddScoped<WolverineQueryDispatcher>();
        
        return services;
    }
}
