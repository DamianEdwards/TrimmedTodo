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

Console.WriteLine($"Listening on http://localhost:{port}");

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
    }

    await host.StopAsync();
}
