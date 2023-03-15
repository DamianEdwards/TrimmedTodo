using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationExtensions
{
    /// <summary>
    /// Starts the application with awareness of configuration values used by benchmarks & scripts to control the startup behavior.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async Task StartApp(this WebApplication app)
    {
        await app.StartAsync();

        Console.Write("ServerStartupComplete,");
        Console.WriteLine(DateTime.UtcNow.Ticks);

        if (app.Configuration["SHUTDOWN_ON_START"] != "true")
        {
            await app.WaitForShutdownAsync();
        }
    }

    /// <summary>
    /// Starts the application with awareness of configuration values used by benchmarks & scripts to control the startup behavior.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="firstRequestPath"></param>
    /// <returns></returns>
    public static async Task StartApp(this WebApplication app, PathString firstRequestPath)
    {
        await app.StartApp();

        if (string.Equals(app.Configuration["SHUTDOWN_ON_START"], "true", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(app.Configuration["SUPPRESS_FIRST_REQUEST"], "true", StringComparison.OrdinalIgnoreCase))
            {
                var server = app.Services.GetRequiredService<IServer>();
                var addresses = server.Features.Get<IServerAddressesFeature>();
                var url = addresses?.Addresses.FirstOrDefault();

                if (url is not null)
                {
                    {
                        using var http = new HttpClient();
                        var response = await http.GetAsync(url + firstRequestPath);
                        response.EnsureSuccessStatusCode();
                    }

                    Console.Write("FirstRequestComplete,");
                    Console.WriteLine(DateTime.UtcNow.Ticks);

                    Console.Write("Environment.WorkingSet,");
                    Console.Write(DateTime.UtcNow.Ticks);
                    Console.Write(",");
                    Console.WriteLine(Environment.WorkingSet);
                }
            }

            await app.StopAsync();
        }
    }
}
