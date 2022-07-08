using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;

using var http = new HttpClient();
http.BaseAddress = new Uri("http://localhost:5079/api/todos/");

await ListCurrentTodos(http);

await AddTodo(http, "Do the groceries");
await AddTodo(http, "Give the dog a bath");
await AddTodo(http, "Wash the car");

Console.WriteLine();

await ListCurrentTodos(http);

await MarkComplete(http, "Wash the car");

await ListCurrentTodos(http);

// TODO: Requires an auth token so expect this to fail for now
try
{
    var deletedCount = await DeleteAllTodos(http);
    Console.WriteLine($"Deleted all {deletedCount} todos!");
    Console.WriteLine();
}
catch (HttpRequestException ex)
{
    if (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        Console.WriteLine("Request to delete todos failed as unauthorized as expected");
    }
    else
    {
        Console.WriteLine($"Unexpected response when deleting todos: {ex.StatusCode}");
    }
}

static async Task ListCurrentTodos(HttpClient http)
{
    var todos = await http.GetFromJsonAsync<List<Todo>>("");

    if (todos is not { Count: >0 })
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

static async Task AddTodo(HttpClient http, string title)
{
    var todo = new Todo() { Title = title };
    var response = await http.PostAsJsonAsync("", todo);
    var createdTodo = await response.Content.ReadFromJsonAsync<Todo>();

    if (response is not { StatusCode: HttpStatusCode.Created } || createdTodo is null)
    {
        throw new InvalidOperationException($"Error creating todo: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }

    Console.WriteLine($"Added todo {createdTodo.Id}");
}

static async Task MarkComplete(HttpClient http, string title)
{
    var todo = await http.GetFromJsonAsync<Todo>($"find?title={Uri.EscapeDataString(title)}");

    if (todo is null)
    {
        throw new InvalidOperationException($"No incomplete todo with title '{title}' was found!");
    }

    var response = await http.PutAsync($"{todo.Id}/mark-complete", null);

    if (response is not { StatusCode: HttpStatusCode.NoContent })
    {
        throw new InvalidOperationException($"Error marking todo complete: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }

    Console.WriteLine($"Todo '{title}' completed!");
    Console.WriteLine();
}

static async Task<int> DeleteAllTodos(HttpClient http)
{
    return await http.DeleteFromJsonAsync<int>("delete-all");
}

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsCompleted { get; set; }
}
