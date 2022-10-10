using System.Buffers;
using System.Net;
using System.Text;

var port = 5003;

using var server = new HttpListener();
server.Prefixes.Add($"http://localhost:{port}/");
server.Start();

Console.Write("ServerStartupComplete,");
Console.Write(DateTime.UtcNow.Ticks);
Console.Write(",http://localhost:");
Console.WriteLine(port);

var stopTokenSource = new CancellationTokenSource();

var requestProcessingTask = Task.Run(ProcessRequests, stopTokenSource.Token);

Console.CancelKeyPress += async (object? sender, ConsoleCancelEventArgs e) =>
{
    await Shutdown();
};

if (Environment.GetEnvironmentVariable("SHUTDOWN_ON_START") != "true")
{
    Console.WriteLine("Press Ctrl+C to exit");
    Console.ReadLine();
}
else
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_FIRST_REQUEST") != "true")
    {
        using var http = new HttpClient();
        try
        {
            var response = await http.GetAsync($"http://localhost:{port}");
            response.EnsureSuccessStatusCode();

            Console.Write("FirstRequestComplete,");
            Console.WriteLine(DateTime.UtcNow.Ticks);

            Console.Write("Environment.WorkingSet,");
            Console.Write(DateTime.UtcNow.Ticks);
            Console.Write(",");
            Console.WriteLine(Environment.WorkingSet);

            await Shutdown(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred: " + ex.ToString());
            await Shutdown(1);
        }
    }
}

async Task ProcessRequests()
{
    while (!stopTokenSource.Token.IsCancellationRequested)
    {
        var context = await server.GetContextAsync();
        var request = context.Request;
        var response = context.Response;

        var responseText = "Hello World!";
        var responseBuffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(responseText));
        var filled = Encoding.UTF8.GetBytes(responseText, 0, responseText.Length, responseBuffer, 0);
        
        response.ContentType = "text/plain";
        response.ContentLength64 = filled;
        try
        {
            await response.OutputStream.WriteAsync(responseBuffer, 0, filled, stopTokenSource.Token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(responseBuffer);
        }
    }

    server.Stop();
}

async Task Shutdown(int exitCode = 0)
{
    Console.WriteLine("Shutting down");
    stopTokenSource.Cancel();
    await requestProcessingTask.WaitAsync(TimeSpan.FromMilliseconds(2000));
    Environment.Exit(exitCode);
}
