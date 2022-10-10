using System.Buffers;
using System.Net;
using System.Text;

var port = 5003;
var shuttingDown = 0L;

using var server = new HttpListener();
server.Prefixes.Add($"http://localhost:{port}/");
server.Start();

Console.Write("ServerStartupComplete,");
Console.Write(DateTime.UtcNow.Ticks);
Console.Write(",http://localhost:");
Console.WriteLine(port);

var stopTokenSource = new CancellationTokenSource();
var shutdownTcs = new TaskCompletionSource();

var requestProcessingTask = Task.Run(StartRequestLoop, stopTokenSource.Token);

if (Environment.GetEnvironmentVariable("SHUTDOWN_ON_START") != "true")
{
    Console.CancelKeyPress += (_, __) => Shutdown();

    Console.WriteLine("Press Ctrl+C to exit");
    await shutdownTcs.Task;
    return 0;
}
else
{
    var exitCode = 0;
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
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred: " + ex.ToString());
            exitCode = 1;
        }
    }

    await Shutdown();
    return exitCode;
}

async Task<long> StartRequestLoop()
{
    long requestsProcessed = 0;
    while (!stopTokenSource.Token.IsCancellationRequested)
    {
        try
        {
            var context = await server.GetContextAsync().ContinueWith(t => t.GetAwaiter().GetResult(), stopTokenSource.Token);
            Task.Run(async () =>
            {
                await ProcessRequest(context);
                Interlocked.Increment(ref requestsProcessed);
            });
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }
    return requestsProcessed;
}

async Task ProcessRequest(HttpListenerContext context)
{
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
    catch (TaskCanceledException)
    {

    }
    finally
    {
        ArrayPool<byte>.Shared.Return(responseBuffer);
    }
}

async Task Shutdown()
{
    if (Interlocked.CompareExchange(ref shuttingDown, 1, 0) != 0)
    {
        return;
    }

    Console.WriteLine("Shutting down");
    stopTokenSource.Cancel();
    var requestsProcessed = await requestProcessingTask.WaitAsync(TimeSpan.FromMilliseconds(2000));
    server.Stop();
    Console.WriteLine($"Server shut down successfully after processing {requestsProcessed} requests");
    shutdownTcs.SetResult();
}
