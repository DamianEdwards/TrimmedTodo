using TrimmedTodo.ApiClient;

const string BaseAddressConfigKey = "baseAddress";

var builder = Host.CreateApplicationBuilder(args);

if (builder.Configuration[BaseAddressConfigKey] is null)
{
    var defaultBaseAddress = "http://localhost:5079/api/todos/";
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { { BaseAddressConfigKey, defaultBaseAddress } });
}

builder.Services.AddHttpClient<TodoApiClient>(ConfigureHttpClient);
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.Run();

void ConfigureHttpClient(HttpClient httpClient)
{
    var token = AuthTokenHelper.GetAuthToken();
    httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

    var baseAddress = builder.Configuration[BaseAddressConfigKey]
        ?? throw new InvalidOperationException($"Required configuration value for key '{BaseAddressConfigKey}' was not found.");
    httpClient.BaseAddress = new Uri(baseAddress);
}

class Worker : BackgroundService
{
    private readonly TodoApiClient _client;
    private readonly ILogger<Worker> _logger;

    public Worker(TodoApiClient client, ILogger<Worker> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
