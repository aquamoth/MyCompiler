using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MyCompiler.Tests
{
    internal class XUnitLogger<T> : ILogger<T>, IDisposable
    {
        private readonly ITestOutputHelper outputHelper;

        public XUnitLogger(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new XUnitLogger<TState>(outputHelper);
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception != null)
                outputHelper.WriteLine(exception.ToString());
            else
                outputHelper.WriteLine(formatter(state, exception));

        }
    }
}