using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using Dapper;
using MiniValidation;

namespace Microsoft.AspNetCore.Routing;

public static class TodoApi
{
    public static RouteGroupBuilder MapTodoApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/todos");

        group.WithTags("Todos");

        group.MapGet("/", async (IDbConnection db) =>
            await db.QueryAsync<Todo>("SELECT * FROM Todos"))
            .WithName("GetAllTodos");

        group.MapGet("/complete", async (IDbConnection db) =>
            await db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = true"))
            .WithName("GetCompleteTodos");

        group.MapGet("/incomplete", async (IDbConnection db) =>
            await db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = false"))
            .WithName("GetIncompleteTodos");

        group.MapGet("/{id:int}", async Task<Results<Ok<Todo>, NotFound>> (int id, IDbConnection db) =>
            await db.QuerySingleOrDefaultAsync<Todo>("SELECT * FROM Todos WHERE Id = @id", new { id })
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
            .WithName("GetTodoById");

        group.MapGet("/find", async Task<Results<Ok<Todo>, NoContent>> (string title, bool? isComplete, IDbConnection db) =>
            await db.QuerySingleOrDefaultAsync<Todo>("SELECT * FROM Todos WHERE Title = @title COLLATE NOCASE AND IsComplete = @isComplete", new { title, isComplete = isComplete ?? false })
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NoContent())
            .WithName("FindTodo");

        group.MapPost("/", async Task<Results<Created<Todo>, ValidationProblem>> (Todo todo, IDbConnection db) =>
            {
                if (!MiniValidator.TryValidate(todo, out var errors))
                    return TypedResults.ValidationProblem(errors);

                var createdTodo = await db.QuerySingleAsync<Todo>(
                    "INSERT INTO Todos(Title, IsComplete) Values(@Title, @IsComplete) RETURNING * ", todo);

                return TypedResults.Created($"/todos/{createdTodo.Id}", createdTodo);
            })
            .WithName("CreateTodo");

        group.MapPut("/{id}", async Task<Results<NoContent, NotFound, ValidationProblem>> (int id, Todo inputTodo, IDbConnection db) =>
            {
                inputTodo.Id = id;

                if (!MiniValidator.TryValidate(inputTodo, out var errors))
                    return TypedResults.ValidationProblem(errors);

                return await db.ExecuteAsync("UPDATE Todos SET Title = @Title, IsComplete = @IsComplete WHERE Id = @Id", inputTodo) == 1
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            })
            .WithName("UpdateTodo");

        group.MapPut("/{id}/mark-complete", async Task<Results<NoContent, NotFound>> (int id, IDbConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = true WHERE Id = @id", new { id }) == 1
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound())
            .WithName("MarkComplete");

        group.MapPut("/{id}/mark-incomplete", async Task<Results<NoContent, NotFound>> (int id, IDbConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = false WHERE Id = @id", new { id }) == 1
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound())
            .WithName("MarkIncomplete");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, IDbConnection db) =>
            await db.ExecuteAsync("DELETE FROM Todos WHERE Id = @id", new { id }) == 1
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound())
            .WithName("DeleteTodo");

        group.MapDelete("/delete-all", async (IDbConnection db) => TypedResults.Ok(await db.ExecuteAsync("DELETE FROM Todos")))
            .WithName("DeleteAll")
            .WithOpenApi(op => new OpenApiOperation(op).WithSecurityRequirementReference(JwtBearerDefaults.AuthenticationScheme))
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

        return group;
    }
}

class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}
