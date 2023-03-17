//using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.OpenApi.Models;
//using MiniValidation;
using Npgsql;

namespace Microsoft.AspNetCore.Routing;

public static class TodoApi
{
    public static RouteGroupBuilder MapTodoApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/todos");

        group.WithTags("Todos");

        group.MapGet("/", (NpgsqlConnection db) => db.QueryAsync<Todo>(@"SELECT * FROM Todos"))
            .WithName("GetAllTodos");

        group.MapGet("/complete", (NpgsqlConnection db) => db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = true"))
            .WithName("GetCompleteTodos");

        group.MapGet("/incomplete", (NpgsqlConnection db) => db.QueryAsync<Todo>("SELECT * FROM Todos WHERE IsComplete = false"))
            .WithName("GetIncompleteTodos");

        group.MapGet("/{id:int}", async Task<Results<Ok<Todo>, NotFound>> (int id, NpgsqlConnection db) =>
            await db.QuerySingleAsync<Todo>("SELECT * FROM Todos WHERE Id = @id", id.AsTypedDbParameter())
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
            .WithName("GetTodoById");

        group.MapGet("/find", async Task<Results<Ok<Todo>, NotFound>> (string title, bool? isComplete, NpgsqlConnection db) =>
            await db.QuerySingleAsync<Todo>("SELECT * FROM Todos WHERE LOWER(Title) = LOWER(@title) AND (@isComplete is NULL OR IsComplete = @isComplete)",
                title.AsTypedDbParameter(), isComplete.AsTypedDbParameter())
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
            .WithName("FindTodo");

        group.MapPost("/", async Task<Results<Created<Todo>, ValidationProblem>> (Todo todo, NpgsqlConnection db) =>
        {
            //if (!MiniValidator.TryValidate(todo, out var errors))
            //    return TypedResults.ValidationProblem(errors);

            var createdTodo = await db.QuerySingleAsync<Todo>(
                "INSERT INTO Todos(Title, IsComplete) Values(@Title, @IsComplete) RETURNING *",
                todo.Title.AsTypedDbParameter(), todo.IsComplete.AsTypedDbParameter());

            return TypedResults.Created($"/todos/{createdTodo?.Id}", createdTodo);
        })
        .WithName("CreateTodo");

        group.MapPut("/{id}", async Task<Results<NoContent, NotFound, ValidationProblem>> (int id, Todo inputTodo, NpgsqlConnection db) =>
        {
            inputTodo.Id = id;

            //return !MiniValidator.TryValidate(inputTodo, out var errors)
            //    ? (Results<NoContent, NotFound, ValidationProblem>)TypedResults.ValidationProblem(errors)
            return await db.ExecuteAsync("UPDATE Todos SET Title = @Title, IsComplete = @IsComplete WHERE Id = @Id",
                                        inputTodo.Title.AsTypedDbParameter(), inputTodo.IsComplete.AsTypedDbParameter()) == 1
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
        })
        .WithName("UpdateTodo");

        group.MapPut("/{id}/mark-complete", async Task<Results<NoContent, NotFound>> (int id, NpgsqlConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = true WHERE Id = @id", id.AsTypedDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("MarkComplete");

        group.MapPut("/{id}/mark-incomplete", async Task<Results<NoContent, NotFound>> (int id, NpgsqlConnection db) =>
            await db.ExecuteAsync("UPDATE Todos SET IsComplete = false WHERE Id = @id", id.AsTypedDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("MarkIncomplete");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, NpgsqlConnection db) =>
            await db.ExecuteAsync("DELETE FROM Todos WHERE Id = @id", id.AsTypedDbParameter()) == 1
                ? TypedResults.NoContent()
                : TypedResults.NotFound())
        .WithName("DeleteTodo");

        group.MapDelete("/delete-all", async (NpgsqlConnection db) => TypedResults.Ok(await db.ExecuteAsync("DELETE FROM Todos")))
            .WithName("DeleteAll");
            //.WithOpenApi(op => new OpenApiOperation(op).WithSecurityRequirementReference(JwtBearerDefaults.AuthenticationScheme))
            //.RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

        return group;
    }
}

sealed class Todo : IDataReaderMapper<Todo>
{
    public int Id { get; set; }
    //[Required]
    public string Title { get; set; } = default!;
    public bool IsComplete { get; set; }

    public static Todo Map(NpgsqlDataReader dataReader)
    {
        return !dataReader.HasRows ? new() : new()
        {
            Id = dataReader.GetInt32(nameof(Id)),
            Title = dataReader.GetString(nameof(Title)),
            IsComplete = dataReader.GetBoolean(nameof(IsComplete))
        };
    }
}

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(IAsyncEnumerable<Todo>))]
[JsonSerializable(typeof(IEnumerable<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
