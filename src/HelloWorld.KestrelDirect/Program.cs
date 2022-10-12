var port = args.Length > 0 && args[0] is { } arg0 && int.TryParse(arg0, out var argPort) ? argPort : 8080;
var responseText = "Hello World"u8.ToArray();
using var loggerFactory = new SlimConsoleLoggerFactory();

using var server = new HttpServer(port, loggerFactory);
server.OnRequest(async context =>
{
    context.Response.ContentType = "text/plain";
    context.Response.ContentLength = responseText.Length;
    await context.Response.BodyWriter.WriteAsync(responseText.AsMemory(), context.RequestAborted);
});

var stopTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, a) =>
{
    a.Cancel = true;
    stopTokenSource.Cancel();
};

if (Environment.GetEnvironmentVariable("SHUTDOWN_ON_START") != "true")
{
    await server.RunAsync(stopTokenSource.Token);
}
else
{
    await server.StartAsync(stopTokenSource.Token);

    if (Environment.GetEnvironmentVariable("SUPPRESS_FIRST_REQUEST") != "true")
    {
        {
            using var http = new HttpClient();
            var response = await http.GetAsync($"http://localhost:{port}");
            response.EnsureSuccessStatusCode();
        }

        Console.Write("FirstRequestComplete,");
        Console.WriteLine(DateTime.UtcNow.Ticks);

        Console.Write("Environment.WorkingSet,");
        Console.Write(DateTime.UtcNow.Ticks);
        Console.Write(",");
        Console.WriteLine(Environment.WorkingSet);
    }

    await server.StopAsync();
}
