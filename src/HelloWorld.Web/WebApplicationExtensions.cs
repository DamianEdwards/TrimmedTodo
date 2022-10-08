using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationExtensions
{
    /// <summary>
    /// Starts the application with awareness of configuration values used by benchmarks & scripts to control the startup behavior.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static async Task StartApp(this WebApplication app, PathString firstRequestPath = default)
    {
        await app.StartAsync();

        Console.Write("ServerStartupComplete,");
        Console.Write(DateTime.UtcNow.Ticks);

        if (app.Configuration["SHUTDOWN_ON_START"] != "true")
        {
            await app.WaitForShutdownAsync();
        }
        else
        {
            if (app.Configuration["SUPPRESS_FIRST_REQUEST"] != "true")
            {
                var server = app.Services.GetRequiredService<IServer>();
                var addresses = server.Features.Get<IServerAddressesFeature>();
                var url = addresses?.Addresses.FirstOrDefault();

                if (url is not null)
                {
                    using var http = new HttpClient();
                    var response = await http.GetAsync(url + firstRequestPath);

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
            }

            await app.StopAsync();
        }
    }
}
