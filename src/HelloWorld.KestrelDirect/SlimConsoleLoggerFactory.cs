using System.Threading.Channels;

class SlimConsoleLoggerFactory : ILoggerFactory
{
    private readonly Channel<Action> _writeChannel = Channel.CreateUnbounded<Action>();
    private readonly Task _writeTask;

    public SlimConsoleLoggerFactory()
    {
        _writeTask = Task.Run(async () =>
        {
            await foreach(var write in _writeChannel.Reader.ReadAllAsync())
            {
                write();
            }
        });
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotSupportedException();
    }

    public ILogger CreateLogger(string categoryName) => new Logger(Write, categoryName);

    public void Dispose()
    {
        _writeChannel.Writer.TryComplete();
        _writeTask.Wait();
    }

    private void Write(Action action)
    {
        _writeChannel.Writer.TryWrite(action);
    }

    class Logger : ILogger
    {
        private readonly Action<Action> _write;

        public Logger(Action<Action> write, string categoryName)
        {
            _write = write;
            CategoryName = categoryName;
        }

        public string CategoryName { get; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _write(() => Console.WriteLine($"{DateTime.Now:O} {logLevel} {eventId}{Environment.NewLine}{formatter(state, exception)}"));
        }
    }
}
