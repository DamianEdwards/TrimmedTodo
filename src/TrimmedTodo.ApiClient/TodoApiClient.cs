using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TrimmedTodo.ApiClient;

public class TodoApiClient
{
    private readonly HttpClient _httpClient;

    public TodoApiClient(string baseAddress)
        : this(new(), baseAddress)
    {

    }

    public TodoApiClient(HttpClient httpClient)
        : this(httpClient, null)
    {

    }

    private TodoApiClient(HttpClient httpClient, string? baseAddress)
    {
        InitializeHttpClient(httpClient, baseAddress);

        _httpClient = httpClient;
    }

    public string? AuthToken
    {
        get => _httpClient.DefaultRequestHeaders.Authorization?.Parameter;
        set
        {
            if (value is not null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", value);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
    }

    private static HttpClient InitializeHttpClient(HttpClient httpClient, string? baseAddress)
    {
        if (httpClient.BaseAddress is null)
        {
            if (baseAddress is null)
            {
                throw new ArgumentException("URL for Todo API could not be determined. Ensure a URL is being passed as a parameter or set via the BaseAddress property on the passed HttpClient instance.");
            }

            httpClient.BaseAddress = new Uri(baseAddress);
        }

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return httpClient;
    }

    public async Task<IReadOnlyList<Todo>> GetCurrentTodos()
    {
        var todos = await _httpClient.GetFromJsonAsync("incomplete", SourceGenerationContext.Web.ListTodo);

        if (todos is null)
        {
            return ReadOnly.EmptyList<Todo>();
        }

        return todos;
    }

    public async Task<Todo> CreateTodo(string title)
    {
        var todo = new Todo { Title = title };
        var response = await _httpClient.PostAsJsonAsync("", todo, SourceGenerationContext.Web.Todo);
        var createdTodo = await response.Content.ReadFromJsonAsync(SourceGenerationContext.Web.Todo);

        if (response is not { StatusCode: HttpStatusCode.Created } || createdTodo is null)
        {
            throw new InvalidOperationException($"Error creating todo: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        return createdTodo;
    }

    public async Task MarkComplete(string title)
    {
        Todo? todo = null;
        var findResponse = await _httpClient.GetAsync($"find?title={Uri.EscapeDataString(title)}");

        if (findResponse.StatusCode == HttpStatusCode.OK)
        {
            todo = await findResponse.Content.ReadFromJsonAsync(SourceGenerationContext.Web.Todo);
        }

        if (todo is null)
        {
            throw new InvalidOperationException($"No incomplete todo with title '{title}' was found!");
        }

        var putResponse = await _httpClient.PutAsync($"{todo.Id}/mark-complete", null);

        if (putResponse is not { StatusCode: HttpStatusCode.NoContent })
        {
            throw new InvalidOperationException($"Error marking todo complete: {putResponse.StatusCode} {await putResponse.Content.ReadAsStringAsync()}");
        }
    }

    public async Task<int> DeleteAllTodos()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "delete-all");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Web.Int32);
    }

}

