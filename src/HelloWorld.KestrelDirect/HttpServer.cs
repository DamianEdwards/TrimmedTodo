using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;

public class HttpServer : IDisposable
{
    private readonly IServerAddressesFeature _addresses = null!;
    private readonly TaskCompletionSource _completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IServer _server;
    private RequestDelegate _requestHandler = ctx => Task.CompletedTask;

    public IFeatureCollection Features => _server.Features;

    public HttpServer(int listenPort)
        : this(DefaultLoggerFactories.Empty)
    {
        _addresses = Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException($"Missing required feature {nameof(IServerAddressesFeature)}");
        _addresses.Addresses.Add($"http://localhost:{listenPort}");
    }

    public HttpServer(int listenPort, ILoggerFactory loggerFactory)
        : this(loggerFactory)
    {
        _addresses = Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException($"Missing required feature {nameof(IServerAddressesFeature)}");
        _addresses.Addresses.Add($"http://localhost:{listenPort}");
    }

    public HttpServer(string listenAddress)
        : this(DefaultLoggerFactories.Empty)
    {
        _addresses = Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException($"Missing required feature {nameof(IServerAddressesFeature)}");
        _addresses.Addresses.Add(listenAddress);
    }

    public HttpServer(string listenAddress, ILoggerFactory loggerFactory)
        : this(loggerFactory)
    {
        _addresses = Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException($"Missing required feature {nameof(IServerAddressesFeature)}");
        _addresses.Addresses.Add(listenAddress);
    }

    public HttpServer(IEnumerable<string> listenAddresses, ILoggerFactory loggerFactory)
        : this(loggerFactory)
    {
        _addresses = Features.Get<IServerAddressesFeature>() ?? throw new InvalidOperationException($"Missing required feature {nameof(IServerAddressesFeature)}");
        foreach (var uri in listenAddresses)
        {
            _addresses.Addresses.Add(uri);
        };
    }

    private HttpServer(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger(nameof(HttpServer));
        _server = new KestrelServer(
            KestrelOptions.Defaults,
            new SocketTransportFactory(SocketOptions.Defaults, _loggerFactory),
            _loggerFactory);
    }

    public void OnRequest(RequestDelegate requestHandler)
    {
        _requestHandler = requestHandler;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Listening on:");
        foreach (var address in _addresses.Addresses)
        {
            sb.AppendLine($"=> {address}");
        }

        return sb.ToString();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _server.StartAsync(new HttpApp(_requestHandler, _logger), cancellationToken);

        _logger.LogInformation(ToString());
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping");

        await _server.StopAsync(default);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);

        cancellationToken.Register(static (o) => ((HttpServer)o!)._completion.TrySetResult(), this);

        await _completion.Task;

        await StopAsync();
    }

    void IDisposable.Dispose() => _server.Dispose();

    internal sealed class HttpApp : IHttpApplication<HttpApp.Context>
    {
        private readonly RequestDelegate _application;
        private readonly ILogger _logger;

        public HttpApp(RequestDelegate application, ILogger logger)
        {
            _application = application;
            _logger = logger;
        }

        // Set up the request
        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            Context? hostContext;
            if (contextFeatures is IHostContextContainer<Context> container)
            {
                hostContext = container.HostContext;
                if (hostContext is null)
                {
                    hostContext = new Context();
                    container.HostContext = hostContext;
                }
            }
            else
            {
                // Server doesn't support pooling, so create a new Context
                hostContext = new Context();
            }

            var httpContext = new DefaultHttpContext(contextFeatures);
            hostContext.HttpContext = httpContext;

            return hostContext;
        }

        // Execute the request
        public Task ProcessRequestAsync(Context context)
        {
            return _application(context.HttpContext!);
        }

        // Clean up the request
        public void DisposeContext(Context context, Exception? exception)
        {
            if (context.HttpContext is DefaultHttpContext httpContext)
            {
                httpContext.Uninitialize();
            }

            // Reset the context as it may be pooled
            context.Reset();
        }

        internal sealed class Context
        {
            public HttpContext? HttpContext { get; set; }
            public IDisposable? Scope { get; set; }

            public long StartTimestamp { get; set; }

            public void Reset()
            {
                // Not resetting HttpContext here as we pool it on the Context
                Scope = null;
                StartTimestamp = 0;
            }
        }
    }

    private class DefaultLoggerFactories
    {
        public static ILoggerFactory Empty => new LoggerFactory();
    }

    private class KestrelOptions : IOptions<KestrelServerOptions>
    {
        private KestrelOptions()
        {
            Value = new KestrelServerOptions();
        }

        public static KestrelOptions Defaults { get; } = new KestrelOptions();

        public KestrelServerOptions Value { get; init; }
    }

    private class SocketOptions : IOptions<SocketTransportOptions>
    {
        public static SocketOptions Defaults { get; } = new SocketOptions
        {
            Value = new SocketTransportOptions()
            {
                WaitForDataBeforeAllocatingBuffer = false,
                UnsafePreferInlineScheduling = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1" : false,
            }
        };

        public SocketTransportOptions Value { get; init; } = new SocketTransportOptions();
    }

    private class LoggerOptions : IOptionsMonitor<ConsoleLoggerOptions>
    {
        public static LoggerOptions Default { get; } = new LoggerOptions();

        public ConsoleLoggerOptions CurrentValue { get; } = new ConsoleLoggerOptions();

        public ConsoleLoggerOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener)
            => NullDisposable.Shared;

        private class NullDisposable : IDisposable
        {
            public static NullDisposable Shared { get; } = new NullDisposable();

            public void Dispose() { }
        }
    }
}
