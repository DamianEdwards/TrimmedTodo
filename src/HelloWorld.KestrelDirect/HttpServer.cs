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

    public TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(1);

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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping");

        await _server.StopAsync(cancellationToken == default(CancellationToken)
            ? new CancellationTokenSource(StopTimeout).Token
            : cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);

        cancellationToken.Register(static (o) => ((HttpServer)o!)._completion.TrySetResult(), this);

        await _completion.Task;

        await StopAsync(new CancellationTokenSource(StopTimeout).Token);
    }

    void IDisposable.Dispose() => _server.Dispose();

    internal sealed class HttpApp : IHttpApplication<HttpContext>
    {
        private readonly RequestDelegate _application;
        private readonly ILogger _logger;

        public HttpApp(RequestDelegate application, ILogger logger)
        {
            _application = application;
            _logger = logger;
        }

        // Set up the request
        public HttpContext CreateContext(IFeatureCollection contextFeatures)
        {
            HttpContext? httpContext;

            if (contextFeatures is IHostContextContainer<HttpContext> container)
            {
                httpContext = container.HostContext;
                if (httpContext is DefaultHttpContext defaultHttpContext)
                {
                    defaultHttpContext.Initialize(contextFeatures);
                }
                else
                {
                    httpContext = new DefaultHttpContext(contextFeatures);
                    container.HostContext = httpContext;
                }
                return httpContext;
            }

            throw new InvalidOperationException("How the world");
        }

        // Execute the request
        public Task ProcessRequestAsync(HttpContext context)
        {
            return _application(context!);
        }

        // Clean up the request
        public void DisposeContext(HttpContext context, Exception? exception)
        {
            if (context is DefaultHttpContext httpContext)
            {
                httpContext.Uninitialize();
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
