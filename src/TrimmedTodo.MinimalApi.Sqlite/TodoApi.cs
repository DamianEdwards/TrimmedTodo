using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using MiniValidation;

namespace Microsoft.AspNetCore.Routing;

public static class TodoApi
{
    public static RouteGroupBuilder MapTodoApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/todos");

        group.WithTags("Todos");

        group.MapGet("/", (SqliteConnection db) => db.QueryAsync<Todo>("SELECT * FROM Todos"))
            .WithName("GetAllTodos");

        group.MapGet("/complete", (SqliteConnection db) => db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = true"))
            .WithName("GetCompleteTodos");

        group.MapGet("/incomplete", (SqliteConnection db) => db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = false"))
            .WithName("GetIncompleteTodos");

        group.MapGet("/{id:int}", async Task<Results<Ok<Todo>, NotFound>> (int id, SqliteConnection db) =>
            await db.QuerySingleAsync<Todo>("SELECT * FROM Todos WHERE Id = @id", id.AsDbParameter())
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
            .WithName("GetTodoById");

        group.MapGet("/find", async Task<Results<Ok<Todo>, NoContent>> (string title, bool? isComplete, SqliteConnection db) =>
            await db.QuerySingleAsync<Todo>("SELECT * FROM Todos WHERE Title = @title COLLATE NOCASE AND (@isComplete is NULL OR IsComplete = @isComplete)",
                title.AsDbParameter(), isComplete.AsDbParameter())
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NoContent())
            .WithName("FindTodo");

        group.MapPost("/", async Task<Results<Created<Todo>, ValidationProblem>> (Todo todo, SqliteConnection db) =>
        {
            if (!MiniValidator.TryValidate(todo, out var errors))
                return TypedResults.ValidationProblem(errors);

            var createdTodo = await db.QuerySingleAsync<Todo>(
                "INSERT INTO Todos(Title, IsComplete) Values(@Title, @IsComplete) RETURNING *",
                todo.Title.AsDbParameter(), todo.IsComplete.AsDbParameter());

            return TypedResults.Created($"/todos/{createdTodo?.Id}", createdTodo);
        })
        .WithName("CreateTodo");

        group.MapPut("/{id}", async Task<Results<NoContent, NotFound, ValidationProblem>> (int id, Todo inputTodo, SqliteConnection db) =>
        {
            inputTodo.Id = id;

            if (!MiniValidator.TryValidate(inputTodo, out var errors))
                return TypedResults.ValidationProblem(errors);

            return await db.ExecuteAsync("UPDATE Todos SET Title = @Title, IsComplete = @IsComplete WHERE Id = @Id",
                inputTodo.Title.AsDbParameter(), inputTodo.IsComplete.AsDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        })
        .WithName("UpdateTodo");

        group.MapPut("/{id}/mark-complete", async Task<Results<NoContent, NotFound>> (int id, SqliteConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = true WHERE Id = @id", id.AsDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("MarkComplete");

        group.MapPut("/{id}/mark-incomplete", async Task<Results<NoContent, NotFound>> (int id, SqliteConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = false WHERE Id = @id", id.AsDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("MarkIncomplete");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, SqliteConnection db) =>
            await db.ExecuteAsync("DELETE FROM Todos WHERE Id = @id", id.AsDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("DeleteTodo");

        group.MapDelete("/delete-all", async (SqliteConnection db) => TypedResults.Ok(await db.ExecuteAsync("DELETE FROM Todos")))
            .WithName("DeleteAll")
            .WithOpenApi(op => new OpenApiOperation(op).WithSecurityRequirementReference(JwtBearerDefaults.AuthenticationScheme))
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

        return group;
    }
}

sealed class Todo : IDataReaderMapper<Todo>
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = default!;
    public bool IsComplete { get; set; }

    public static Todo Map(SqliteDataReader dataReader)
    {
        return !dataReader.HasRows ? new() : new()
        {
            Id = dataReader.GetInt32(nameof(Id)),
            Title = dataReader.GetString(nameof(Title)),
            IsComplete = dataReader.GetBoolean(nameof(IsComplete))
        };
    }
}
