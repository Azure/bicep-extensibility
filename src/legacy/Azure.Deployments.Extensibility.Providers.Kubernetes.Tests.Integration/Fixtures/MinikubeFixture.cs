// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.Fixtures
{
    public class MinikubeFixture : IAsyncLifetime
    {
        private readonly IMessageSink diagnosticMessageSink;

        private readonly bool runningInCI =
            Environment.GetEnvironmentVariable("CI")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        public MinikubeFixture(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        public async Task InitializeAsync()
        {
            if (this.runningInCI)
            {
                return;
            }

            await this.ExecuteMinikubeCommandAsync("start");
        }

        public async Task DisposeAsync()
        {
            if (this.runningInCI)
            {
                return;
            }

            await this.ExecuteMinikubeCommandAsync("delete");
        }

        private async Task ExecuteMinikubeCommandAsync(string arguments)
        {
            var process = new Process();

            process.StartInfo.FileName = "minikube";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            string standardOutput = "";
            string standardError = "";

            // Using two thread to avoid deadlocks.
            Thread readStandardOutputThread = new(() => { standardOutput = process.StandardOutput.ReadToEnd(); });
            Thread readStandardErrorThread = new(() => { standardError = process.StandardError.ReadToEnd(); });

            readStandardOutputThread.Start();
            readStandardErrorThread.Start();

            await process.WaitForExitAsync();
            readStandardOutputThread.Join();
            readStandardErrorThread.Join();

            var errorMessage = new DiagnosticMessage(standardError);
            this.diagnosticMessageSink.OnMessage(errorMessage);

            var outputMessage = new DiagnosticMessage(standardOutput);
            this.diagnosticMessageSink.OnMessage(errorMessage);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Cannot start minikube.");
            }

        }
    }
}
