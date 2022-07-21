using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

namespace Microsoft.AspNetCore.Routing;

public static class TodoApi
{
    public static RouteGroupBuilder MapTodoApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/todos");

        group.WithTags("Todos");

        group.MapGet("/", async (TodoDb db) =>
            await db.Todos.ToListAsync())
            .WithName("GetAllTodos");

        group.MapGet("/complete", async (TodoDb db) =>
            await db.Todos.Where(t => t.IsComplete).ToListAsync())
            .WithName("GetCompleteTodos");

        group.MapGet("/incomplete", async (TodoDb db) =>
            await db.Todos.Where(t => !t.IsComplete).ToListAsync())
            .WithName("GetIncompleteTodos");

        group.MapGet("/{id:int}", async Task<Results<Ok<Todo>, NotFound>> (int id, TodoDb db) =>
            await db.Todos.FindAsync(id)
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
            .WithName("GetTodoById");

        group.MapGet("/find", async Task<Results<Ok<Todo>, NoContent>> (string title, bool? isComplete, TodoDb db) =>
            await db.Todos.SingleOrDefaultAsync(t => t.Title == title && t.IsComplete == (isComplete ?? false))
                is Todo todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NoContent())
            .WithName("FindTodo");

        group.MapPost("/", async Task<Results<Created<Todo>, ValidationProblem>> (Todo todo, TodoDb db) =>
            {
                if (!MiniValidator.TryValidate(todo, out var errors))
                    return TypedResults.ValidationProblem(errors);

                db.Todos.Add(todo);
                await db.SaveChangesAsync();

                return TypedResults.Created($"/todos/{todo.Id}", todo);
            })
            .WithName("CreateTodo");

        group.MapPut("/{id}", async Task<Results<NoContent, NotFound, ValidationProblem>> (int id, Todo inputTodo, TodoDb db) =>
            {
                if (!MiniValidator.TryValidate(inputTodo, out var errors))
                    return TypedResults.ValidationProblem(errors);

                var todo = await db.Todos.FindAsync(id);

                if (todo is null) return TypedResults.NotFound();

                todo.Title = inputTodo.Title;
                todo.IsComplete = inputTodo.IsComplete;

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            })
            .WithName("UpdateTodo");

        group.MapPut("/{id}/mark-complete", async Task<Results<NoContent, NotFound>> (int id, TodoDb db) =>
            {
                var todo = await db.Todos.FindAsync(id);

                if (todo is null) return TypedResults.NotFound();

                todo.IsComplete = true;

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            })
            .WithName("MarkComplete");

        group.MapPut("/{id}/mark-incomplete", async Task<Results<NoContent, NotFound>> (int id, TodoDb db) =>
            {
                var todo = await db.Todos.FindAsync(id);

                if (todo is null) return TypedResults.NotFound();

                todo.IsComplete = false;

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            })
            .WithName("MarkIncomplete");

        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, TodoDb db) =>
            {
                var todo = await db.Todos.FindAsync(id);

                if (todo is null) return TypedResults.NotFound();

                db.Todos.Remove(todo);

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            })
            .WithName("DeleteTodo");

        group.MapDelete("/delete-all", async (TodoDb db) => TypedResults.Ok(await db.Database.ExecuteSqlRawAsync("DELETE FROM Todos")))
            .WithName("DeleteAll")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("admin"));

        return group;
    }
}

public interface IValueResult<TValue>
{
    TValue Value { get; }
}

class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}

class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options)
    {
        Todos = Set<Todo>();
    }

    public DbSet<Todo> Todos { get; }
}
