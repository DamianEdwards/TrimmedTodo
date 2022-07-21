using TrimmedTodo.ApiClient;

var baseAddress = "http://localhost:5079/api/todos/";

if (args.Length > 0)
{
    if (!Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
    {
        throw new ArgumentException("Invalid base address specified. Ensure the base address is passed as the only argument, " +
            $"e.g. TrimmedTodo.Api.Client {baseAddress}");
    }
    baseAddress = uri.ToString();
}

var token = AuthTokenHelper.GetAuthToken();
var todoApiClient = new TodoApiClient(baseAddress)
{
    AuthToken = token
};

await ListCurrentTodos(todoApiClient);

await AddTodo(todoApiClient, "Do the groceries");
await AddTodo(todoApiClient, "Give the dog a bath");
await AddTodo(todoApiClient, "Wash the car");

Console.WriteLine();

await ListCurrentTodos(todoApiClient);

await MarkComplete(todoApiClient, "Wash the car");

await ListCurrentTodos(todoApiClient);

var deletedCount = await DeleteAllTodos(todoApiClient);
Console.WriteLine($"Deleted all {deletedCount} todos!");
Console.WriteLine();

static async Task ListCurrentTodos(TodoApiClient client)
{
    var todos = await client.GetCurrentTodos();

    if (todos is not { Count: > 0 })
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

static async Task AddTodo(TodoApiClient client, string title)
{
    var todo = await client.CreateTodo(title);

    Console.WriteLine($"Added todo {todo.Id}");
}

static async Task MarkComplete(TodoApiClient client, string title)
{
    await client.MarkComplete(title);

    Console.WriteLine($"Todo '{title}' completed!");
    Console.WriteLine();
}

static async Task<int> DeleteAllTodos(TodoApiClient client)
{
    return await client.DeleteAllTodos();
}
