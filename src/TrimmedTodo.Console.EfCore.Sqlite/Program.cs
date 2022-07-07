using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var db = new TodoDb();

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
    var todos = await db.Todos.Where(t => !t.IsCompleted).ToListAsync();
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
    var todo = new Todo() { Title = title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    Console.WriteLine($"Added todo {todo.Id}");
}

static async Task MarkComplete(TodoDb db, string title)
{
    var todo = await db.Todos.Where(t => t.Title == title && t.IsCompleted == false).SingleOrDefaultAsync();

    if (todo is null)
    {
        throw new InvalidOperationException($"No incomplete todo with title '{title}' was found!");
    }

    todo.IsCompleted = true;

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
    Console.WriteLine($"Ensuring database exists and is up to date at connection string '{db.Database.GetConnectionString()}'");

    await db.Database.MigrateAsync();

    Console.WriteLine();
}

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsCompleted { get; set; }
}

public class TodoDb : DbContext
{
    public TodoDb()
    {
        Todos = Set<Todo>();
    }

    public DbSet<Todo> Todos { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=todos.db");
    }
}
