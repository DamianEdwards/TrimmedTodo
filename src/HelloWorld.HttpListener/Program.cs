using System.Collections.Concurrent;
using System.Net;

var port = args.Length > 0 && args[0] is { } arg0 && int.TryParse(arg0, out var argPort) ? argPort : 5003;
var shuttingDown = 0L;
var responseTextBuffer = "Hello World!"u8.ToArray();

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
    Console.CancelKeyPress += (_, a) =>
    {
        a.Cancel = true;
        Shutdown();
    };

    Console.WriteLine("Press Ctrl+C to exit");
    await shutdownTcs.Task;
    return 0;
}
else
{
    var exitCode = 0;
    if (Environment.GetEnvironmentVariable("SUPPRESS_FIRST_REQUEST") != "true")
    {
        try
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
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred: " + ex.ToString());
            exitCode = 1;
        }
    }

    Shutdown();
    await shutdownTcs.Task;
    return exitCode;
}

async Task<(long, long, long)> StartRequestLoop()
{
    long requestsReceived = 0;
    long requestsProcessed = 0;
    var requestTasks = new ConcurrentQueue<Task>();

    while (!stopTokenSource.Token.IsCancellationRequested)
    {
        try
        {
            var context = await server.GetContextAsync().WaitAsync(stopTokenSource.Token);
            requestsReceived++;

            requestTasks.Enqueue(Task.Run(async () =>
            {
                await ProcessRequest(context);
                Interlocked.Increment(ref requestsProcessed);
            }, stopTokenSource.Token));
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }

    // Drain requests
    var requestsFailed = 0;
    try
    {
        await Task.WhenAll(requestTasks);
    }
    catch (Exception)
    {
        foreach (var requestTask in requestTasks)
        {
            if (requestTask.IsFaulted)
            {
                requestsFailed++;
            }
        }
    }

    return (requestsReceived, requestsProcessed, requestsFailed);
}

async Task ProcessRequest(HttpListenerContext context)
{
    var request = context.Request;
    var response = context.Response;

    response.ContentType = "text/plain";
    response.ContentLength64 = responseTextBuffer.Length;

    try
    {
        await response.OutputStream.WriteAsync(responseTextBuffer.AsMemory(), stopTokenSource.Token);
    }
    catch (TaskCanceledException)
    {

    }
    finally
    {
        response.Close();
    }
}

async void Shutdown()
{
    if (Interlocked.CompareExchange(ref shuttingDown, 1, 0) != 0)
    {
        return;
    }

    Console.WriteLine("Shutting down");
    stopTokenSource.Cancel();
    var (received, processed, failed) = await requestProcessingTask.WaitAsync(TimeSpan.FromMilliseconds(2000));
    server.Stop();
    var numWidth = Math.Max(received, Math.Max(processed, failed)).ToString().Length;
    Console.WriteLine("Server shut down successfully");
    Console.WriteLine($"- {received.ToString().PadLeft(numWidth)} requests received");
    Console.WriteLine($"- {processed.ToString().PadLeft(numWidth)} requests processed");
    Console.WriteLine($"- {failed.ToString().PadLeft(numWidth)} requests failed");
    shutdownTcs.SetResult();
}
