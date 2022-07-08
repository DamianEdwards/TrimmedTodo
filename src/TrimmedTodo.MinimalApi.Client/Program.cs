using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using var http = new HttpClient();
http.BaseAddress = new Uri("http://localhost:5079/api/todos/");
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

await ListCurrentTodos(http);

await AddTodo(http, "Do the groceries");
await AddTodo(http, "Give the dog a bath");
await AddTodo(http, "Wash the car");

Console.WriteLine();

await ListCurrentTodos(http);

await MarkComplete(http, "Wash the car");

await ListCurrentTodos(http);

var deletedCount = await DeleteAllTodos(http);
Console.WriteLine($"Deleted all {deletedCount} todos!");
Console.WriteLine();

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
    var todo = new Todo { Title = title };
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
    var token = GetAuthToken();
    
    var request = new HttpRequestMessage(HttpMethod.Delete, "delete-all");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    var response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<int>();
}

static string GetAuthToken()
{
    var tokenFileName = ".authtoken";
    var tokenFilePath = Path.Combine(AppContext.BaseDirectory, tokenFileName);
    if (!File.Exists(tokenFilePath))
    {
        throw new InvalidOperationException(
            $"File '{tokenFileName}' not found. Run 'dotnet user-jwts create --role admin' " +
            $"in the API project directory to create an auth token and save it in a file named '{tokenFileName}' " +
            $"in the {Process.GetCurrentProcess().ProcessName} project directory.");
    }
    var token = File.ReadAllText(tokenFilePath);
    return token;
}

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsCompleted { get; set; }
}
