var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.StartAsync();

if (builder.Configuration["SHUTDOWN_ON_START"] != "true")
{
    app.WaitForShutdown();
}
else
{
    await app.StopAsync();
}
