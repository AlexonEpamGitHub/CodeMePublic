using Application.Todos.Create;
using Application.Todos.Get;
using Application.Todos.GetById;
using Application.Todos.Complete;
using Application.Todos.Delete;
using Wolverine;
using Wolverine.Http;

namespace Web.Api.Wolverine.Endpoints.Todos;

/// <summary>
/// Create a new todo item
/// </summary>
public static class CreateTodoEndpoint
{
    [WolverinePost("/api/todos")]
    public static async Task<IResult> Handle(
        CreateTodoCommand command,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SharedKernel.Result>(command);
        
        return result.IsSuccess
            ? Results.Created($"/api/todos/{result.Value}", result.Value)
            : Results.BadRequest(result.Error);
    }
}

/// <summary>
/// Get all todos
/// </summary>
public static class GetTodosEndpoint
{
    [WolverineGet("/api/todos")]
    public static async Task<IResult> Handle(
        [FromQuery] bool? isCompleted,
        IMessageBus bus)
    {
        var query = new GetTodosQuery(isCompleted);
        var result = await bus.InvokeAsync<SharedKernel.Result<IReadOnlyCollection<TodoResponse>>>(query);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }
}

/// <summary>
/// Get todo by id
/// </summary>
public static class GetTodoByIdEndpoint
{
    [WolverineGet("/api/todos/{id}")]
    public static async Task<IResult> Handle(
        Guid id,
        IMessageBus bus)
    {
        var query = new GetTodoByIdQuery(id);
        var result = await bus.InvokeAsync<SharedKernel.Result<Application.Todos.GetById.TodoResponse>>(query);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }
}

/// <summary>
/// Complete a todo item
/// </summary>
public static class CompleteTodoEndpoint
{
    [WolverinePut("/api/todos/{id}/complete")]
    public static async Task<IResult> Handle(
        Guid id,
        IMessageBus bus)
    {
        var command = new CompleteTodoCommand(id);
        var result = await bus.InvokeAsync<SharedKernel.Result>(command);
        
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(result.Error);
    }
}

/// <summary>
/// Delete a todo item
/// </summary>
public static class DeleteTodoEndpoint
{
    [WolverineDelete("/api/todos/{id}")]
    public static async Task<IResult> Handle(
        Guid id,
        IMessageBus bus)
    {
        var command = new DeleteTodoCommand(id);
        var result = await bus.InvokeAsync<SharedKernel.Result>(command);
        
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error);
    }
}
