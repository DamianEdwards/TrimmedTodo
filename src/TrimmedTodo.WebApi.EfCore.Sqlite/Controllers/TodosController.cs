using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrimmedTodo.WebApi.EfCore.Sqlite.Data;

namespace TrimmedTodo.WebApi.EfCore.Sqlite.Controllers;

[Route("api/[controller]")]
[ApiController]
[ApiConventionType(typeof(DefaultApiConventions))]
public class TodosController : ControllerBase
{
    private readonly TodoDb _db;

    public TodosController(TodoDb db)
    {
        _db = db;
    }

    // GET: api/<TodoController>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Todo>>> GetAllTodos() =>
        await _db.Todos.ToListAsync();

    // GET api/<TodoController>/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Todo>> GetTodoById(int id) =>
        await _db.Todos.FindAsync(id)
            is Todo todo
                ? todo
                : NotFound();

    // GET api/<TodoController>/find?title=example
    [HttpGet("find")]
    public async Task<ActionResult<Todo>> FindTodo(string title, bool? isComplete) =>
        await _db.Todos.SingleOrDefaultAsync(t => t.Title == title && t.IsComplete == (isComplete ?? false))
            is Todo todo
                ? todo
                : NotFound();

    // POST api/<TodoController>
    [HttpPost]
    public async Task<ActionResult<Todo>> CreateTodo(Todo todo)
    {
        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
    }

    // PUT api/<TodoController>/5
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTodo(int id, Todo todo)
    {
        var exisingTodo = await _db.Todos.FindAsync(id);

        if (exisingTodo is null) return NotFound();

        exisingTodo.Title = todo.Title;
        exisingTodo.IsComplete = todo.IsComplete;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // PUT api/<TodoController>/5/mark-complete
    [HttpPut("{id}/mark-complete")]
    public async Task<ActionResult> MarkComplete(int id)
    {
        var exisingTodo = await _db.Todos.FindAsync(id);

        if (exisingTodo is null) return NotFound();

        exisingTodo.IsComplete = true;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE api/<TodoController>/5
    [HttpDelete("delete-all")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<int>> DeleteAll() => await _db.Database.ExecuteSqlRawAsync("DELETE FROM Todos");
}
