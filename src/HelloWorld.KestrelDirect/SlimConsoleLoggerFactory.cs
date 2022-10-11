using System.Collections.Concurrent;
using System.Diagnostics;

class SlimConsoleLoggerFactory : ILoggerFactory
{
    private readonly ConcurrentQueue<Action> _writeQueue = new();
    private Task _writeTask = Task.CompletedTask;
    private long _writing = 0;

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotSupportedException();
    }

    public ILogger CreateLogger(string categoryName) => new Logger(Write, categoryName);

    public Task FlushAsync() => _writeTask;

    public void Dispose()
    {
        //_writeTask.Wait();
    }

    private async void Write(Action action)
    {
        _writeQueue.Enqueue(action);

        if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
        {
            // Kick-off write task
            await _writeTask;
            _writeTask = Task.Run(() =>
            {
                while (_writeQueue.TryDequeue(out var write))
                {
                    write();
                }

                Debug.Assert(Interlocked.CompareExchange(ref _writing, 0, 1) == 1);
            });
        }
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
