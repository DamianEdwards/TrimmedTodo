using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net.Http.Headers;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
    "Server=localhost;Port=5432;User Id=TodosApp;Password=password;Database=Todos";
using var db = new NpgsqlConnection(connectionString);
await db.OpenAsync();

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

static async Task ListCurrentTodos(NpgsqlConnection db)
{
    var todos = await db.QueryAsync<Todo>(
        """
            SELECT *
            FROM "Todos"
            WHERE "IsComplete" = false
        """)
        .ToListAsync();
    
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

static async Task AddTodo(NpgsqlConnection db, string title)
{
    var todo = new Todo { Title = title };

    var createdTodo = await db.QuerySingleAsync<Todo>(
        """
            INSERT INTO "Todos"("Title", "IsComplete")
            Values (@Title, @IsComplete)
            RETURNING *
        """,
        parameters => parameters
            .AddTyped(todo.Title)
            .AddTyped(todo.IsComplete));
    
    Console.WriteLine($"Added todo {createdTodo?.Id}");
}

static async Task MarkComplete(NpgsqlConnection db, string title)
{
    var result = await db.ExecuteAsync(
        """
            UPDATE "Todos"
              SET "IsComplete" = true
            WHERE "Title" = @title
              AND "IsComplete" = false
        """,
        p => p.AddTyped(title));
    
    if (result == 0)
    {
        throw new InvalidOperationException($"No incomplete todo with title '{title}' was found!");
    }

    Console.WriteLine($"Todo '{title}' completed!");
    Console.WriteLine();
}

static async Task<int> DeleteAllTodos(NpgsqlConnection db)
{
    return await db.ExecuteAsync(@"DELETE FROM ""Todos""");
}

async Task EnsureDb(NpgsqlConnection db)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
    {
        Console.WriteLine($"Ensuring database exists and is up to date at connection string '{ObscurePassword(connectionString)}'");

        var sql = $"""
                  CREATE TABLE IF NOT EXISTS public."Todos"
                  (
                      "{nameof(Todo.Id)}" SERIAL PRIMARY KEY,
                      "{nameof(Todo.Title)}" text NOT NULL,
                      "{nameof(Todo.IsComplete)}" boolean NOT NULL DEFAULT false
                  );

                  ALTER TABLE IF EXISTS public."Todos"
                      OWNER to "TodosApp";

                  DELETE FROM "Todos";
                  """;
        await db.ExecuteAsync(sql);

        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"Database initialization disabled for connection string '{ObscurePassword(connectionString)}'");
    }

    string ObscurePassword(string connectionString)
    {
        var passwordKey = "Password=";
        var passwordIndex = connectionString.IndexOf(passwordKey, 0, StringComparison.OrdinalIgnoreCase);
        if (passwordIndex < 0)
        {
            return connectionString;
        }
        var semiColonIndex = connectionString.IndexOf(";", passwordIndex, StringComparison.OrdinalIgnoreCase);
        return connectionString.Substring(0, passwordIndex + passwordKey.Length) +
               "*****" +
               (semiColonIndex >= 0 ? connectionString.Substring(semiColonIndex) : "");
    }
}

sealed class Todo : IDataReaderMapper<Todo>
{
    public int Id { get; init; }
    [Required]
    public required string Title { get; set; }
    public bool IsComplete { get; set; }

    public static Todo Map(NpgsqlDataReader dataReader)
    {
        return new()
        {
            Id = dataReader.GetInt32(nameof(Id)),
            Title = dataReader.GetString(nameof(Title)),
            IsComplete = dataReader.GetBoolean(nameof(IsComplete))
        };
    }
}
