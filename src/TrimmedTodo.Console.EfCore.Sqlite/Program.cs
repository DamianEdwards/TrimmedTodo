using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

using var db = new TodoDb(Environment.GetEnvironmentVariable("CONNECTION_STRING"));

await EnsureDb(db);

await ListCurrentTodos(db);

await AddTodo(db, "Do the groceries");
await AddTodo(db, "Give the dog a bath");
await AddTodo(db, "Wash the car");

Console.WriteLine();

await ListCurrentTodos(db);

await MarkComplete(db, "Wash the car");

await ListCurrentTodos(db);

var deletedCount = await DeleteAllTodos(db);
Console.WriteLine($"Deleted all {deletedCount} todos!");
Console.WriteLine();

static async Task ListCurrentTodos(TodoDb db)
{
    var todos = await db.Todos.Where(t => !t.IsComplete).ToListAsync();
    if (todos.Count == 0)
    {
        Console.WriteLine("There are currently no todos!");
        Console.WriteLine();
        return;
    }

    var idColHeading = "Id";
    var titleColHeading = "Title";
    var idWidth = int.Max(idColHeading.Length, todos.Max(t => t.Id).ToString().Length);
    var titleWidth = int.Max(titleColHeading.Length, todos.Max(t => t.Title?.Length ?? 0));
    var lineWidth = idWidth + 1 + titleWidth;

    Console.Write(idColHeading.PadRight(idWidth));
    Console.Write(" ");
    Console.WriteLine(titleColHeading.PadRight(titleWidth));
    Console.WriteLine(new string('-', lineWidth));

    foreach (var todo in todos)
    {
        Console.Write(todo.Id.ToString().PadRight(idWidth));
        Console.Write(" ");
        Console.WriteLine(todo.Title?.PadRight(titleWidth));
    }

    Console.WriteLine();
}

static async Task AddTodo(TodoDb db, string title)
{
    var todo = new Todo { Title = title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    Console.WriteLine($"Added todo {todo.Id}");
}

static async Task MarkComplete(TodoDb db, string title)
{
    var todo = await db.Todos.Where(t => t.Title == title && t.IsComplete == false).SingleOrDefaultAsync();

    if (todo is null)
    {
        throw new InvalidOperationException($"No incomplete todo with title '{title}' was found!");
    }

    todo.IsComplete = true;

    await db.SaveChangesAsync();

    Console.WriteLine($"Todo '{title}' completed!");
    Console.WriteLine();
}

static async Task<int> DeleteAllTodos(TodoDb db)
{
    return await db.Database.ExecuteSqlRawAsync("DELETE FROM Todos");
}

static async Task EnsureDb(TodoDb db)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
    {
        Console.WriteLine($"Ensuring database exists and is up to date at connection string '{db.Database.GetConnectionString()}'");

        await db.Database.MigrateAsync();

        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"Database initialization disabled for connection string '{db.Database.GetConnectionString()}'");
    }
}

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}

public class TodoDb : DbContext
{
    private readonly string _cs;

    public TodoDb() : this(null) { }

    public TodoDb(string? connectionString)
    {
        _cs = !string.IsNullOrEmpty(connectionString) ? connectionString : "Data Source=todos.db;Cache=Shared";
        Todos = Set<Todo>();
    }

    public DbSet<Todo> Todos { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_cs);
        optionsBuilder.UseModel(TrimmedTodo.Console.EfCore.Sqlite.CompiledModels.TodoDbModel.Instance);
    }
}
