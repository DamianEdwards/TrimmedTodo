using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

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
    var server = app.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>();
    var url = addresses?.Addresses.FirstOrDefault();

    if (url is not null)
    {
        using var http = new HttpClient();
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }

    await app.StopAsync();
}
