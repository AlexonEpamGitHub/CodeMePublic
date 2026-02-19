using Application.Users.Register;
using Application.Users.Login;
using Application.Users.GetById;
using Wolverine;
using Wolverine.Http;

namespace Web.Api.Wolverine.Endpoints.Users;

/// <summary>
/// Register a new user
/// </summary>
public static class RegisterUserEndpoint
{
    [WolverinePost("/api/users/register")]
    public static async Task<IResult> Handle(
        RegisterUserCommand command,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SharedKernel.Result>(command);
        
        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value}", result.Value)
            : Results.BadRequest(result.Error);
    }
}

/// <summary>
/// Login user
/// </summary>
public static class LoginUserEndpoint
{
    public record LoginResponse(string Token);
    
    [WolverinePost("/api/users/login")]
    public static async Task<IResult> Handle(
        LoginUserCommand command,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SharedKernel.Result<string>>(command);
        
        return result.IsSuccess
            ? Results.Ok(new LoginResponse(result.Value))
            : Results.Unauthorized();
    }
}

/// <summary>
/// Get user by id
/// </summary>
public static class GetUserByIdEndpoint
{
    [WolverineGet("/api/users/{id}")]
    public static async Task<IResult> Handle(
        Guid id,
        IMessageBus bus)
    {
        var query = new GetUserByIdQuery(id);
        var result = await bus.InvokeAsync<SharedKernel.Result<UserResponse>>(query);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }
}
