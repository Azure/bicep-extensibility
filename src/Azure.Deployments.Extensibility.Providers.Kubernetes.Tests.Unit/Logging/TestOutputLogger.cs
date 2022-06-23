// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using System.Reactive.Disposables;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Logging
{
    public sealed class TestOutputLogger : ILogger
    {
        public TestOutputLogger(ITestOutputHelper testOutput, string loggerCategory, LogLevel minLogLevel)
        {
            this.TestOutput = testOutput ?? throw new ArgumentNullException(nameof(testOutput));

            if (string.IsNullOrWhiteSpace(loggerCategory))
            {
                throw new ArgumentException(
                    "Argument cannot be null, empty, or entirely composed of whitespace: 'loggerCategory'.",
                    nameof(loggerCategory));
            }

            this.LoggerCategory = loggerCategory;
            this.MinLogLevel = minLogLevel;
        }

        public ITestOutputHelper TestOutput { get; }

        public string LoggerCategory { get; }

        public LogLevel MinLogLevel { get; }

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            try
            {
                this.TestOutput.WriteLine(string.Format(
                    "[{0}] {1}: {2}",
                    level,
                    this.LoggerCategory,
                    formatter(state, exception)));

                if (exception != null)
                {
                    this.TestOutput.WriteLine(
                        exception.ToString());
                }
            }
            catch (Exception e)
            {
                // Ignore 'There is no currently active test.'
                if (e.ToString().Contains("There is no currently active test"))
                {
                    return;
                }

                throw;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.MinLogLevel;

        public IDisposable BeginScope<TState>(TState state) => Disposable.Empty;
    }
}
