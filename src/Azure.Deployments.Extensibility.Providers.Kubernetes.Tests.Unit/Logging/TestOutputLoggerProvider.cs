using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Logging
{
    internal sealed class TestOutputLoggerProvider : ILoggerProvider
    {
        public TestOutputLoggerProvider(ITestOutputHelper testOutput, LogLevel minLogLevel = LogLevel.Information)
        {
            this.TestOutput = testOutput ?? throw new ArgumentNullException(nameof(testOutput));
            this.MinLogLevel = minLogLevel;
        }

        public void Dispose()
        {
        }

        private ITestOutputHelper TestOutput { get; }

        public LogLevel MinLogLevel { get; }

        public ILogger CreateLogger(string categoryName) => new TestOutputLogger(this.TestOutput, categoryName, this.MinLogLevel);
    }
}