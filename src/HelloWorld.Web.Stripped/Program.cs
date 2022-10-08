using System.Diagnostics;

var port = 5000;

var host = new WebHostBuilder()
    .UseKestrel(c => c.ListenLocalhost(port))
    .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    //.ConfigureServices(services => services.AddRouting())
    .Configure(app =>
    {
        app.Run(context => context.Response.WriteAsync("Hello World!"));
        //app.UseRouting();
        //app.UseEndpoints(routes =>
        //{
        //    routes.MapGet("/", () => "Hello World!");
        //});
    })
    .Build();

await host.StartAsync();

Console.Write("ServerStartupComplete,");
Console.Write(DateTime.UtcNow.Ticks);
Console.Write(",http://localhost:");
Console.WriteLine(port);

if (Environment.GetEnvironmentVariable("SHUTDOWN_ON_START") != "true")
{
    await host.WaitForShutdownAsync();
}
else
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_FIRST_REQUEST") != "true")
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"http://localhost:{port}");
        response.EnsureSuccessStatusCode();

        Console.Write("FirstRequestComplete,");
        Console.WriteLine(DateTime.UtcNow.Ticks);

        var process = Process.GetCurrentProcess();

        Console.Write("Process.PrivateMemorySize64,");
        Console.Write(DateTime.UtcNow.Ticks);
        Console.Write(",");
        Console.WriteLine(process.PrivateMemorySize64);

        Console.Write("Process.WorkingSet64,");
        Console.Write(DateTime.UtcNow.Ticks);
        Console.Write(",");
        Console.WriteLine(process.WorkingSet64);

        Console.Write("Process.PeakWorkingSet64,");
        Console.Write(DateTime.UtcNow.Ticks);
        Console.Write(",");
        Console.WriteLine(process.PeakWorkingSet64);
    }

    await host.StopAsync();
}
