using Application.Abstractions.Messaging;
using Wolverine;

namespace Web.Api.Wolverine.Handlers;

/// <summary>
/// Wolverine handler that delegates to application command handlers
/// </summary>
public class CommandHandler
{
    public static async Task<SharedKernel.Result> Handle<TCommand>(
        TCommand command,
        ICommandHandler<TCommand> handler) 
        where TCommand : ICommand
    {
        return await handler.Handle(command, CancellationToken.None);
    }
}

/// <summary>
/// Wolverine handler that delegates to application command handlers with results
/// </summary>
public class CommandHandlerWithResult
{
    public static async Task<SharedKernel.Result<TResult>> Handle<TCommand, TResult>(
        TCommand command,
        ICommandHandler<TCommand, TResult> handler)
        where TCommand : ICommand<TResult>
    {
        return await handler.Handle(command, CancellationToken.None);
    }
}

/// <summary>
/// Wolverine handler that delegates to application query handlers
/// </summary>
public class QueryHandler
{
    public static async Task<SharedKernel.Result<TResult>> Handle<TQuery, TResult>(
        TQuery query,
        IQueryHandler<TQuery, TResult> handler)
        where TQuery : IQuery<TResult>
    {
        return await handler.Handle(query, CancellationToken.None);
    }
}
