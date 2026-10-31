// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TypeGeneration.TestGenerator;

[Generator]
public sealed class GeneratedProperties : IIncrementalGenerator
{
    private static int onceExecutions;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mode = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
            options.GlobalOptions.TryGetValue("build_property.TypeGenerationTestMode", out var value) ? value : "success");
        var additionalTexts = context.AdditionalTextsProvider.Select((text, token) => text.GetText(token)?.ToString()).Collect();
        context.RegisterSourceOutput(mode.Combine(additionalTexts), static (output, input) =>
        {
            var value = input.Left;
            if (value == "once" && Interlocked.Increment(ref onceExecutions) != 1)
            {
                throw new InvalidOperationException("TH08 generator was executed more than once.");
            }

            if (value == "inputs" && !input.Right.Contains("TH08 additional input"))
            {
                throw new InvalidOperationException("TH08 AdditionalFiles were not supplied.");
            }

            if (value == "throw")
            {
                throw new InvalidOperationException("Intentional TH08 generator failure.");
            }

            if (value == "omit")
            {
                return;
            }

            if (value is "error" or "warning")
            {
                output.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    value == "error" ? "TH0801" : "TH0802",
                    "Generator compatibility fixture",
                    "Intentional TH08 generator diagnostic",
                    "TypeGenerationTests",
                    value == "error" ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                    isEnabledByDefault: true), Location.None));
            }

            var source = $$"""
                #nullable enable
                namespace GeneratedModels;

                public partial class Repository
                {
                    [global::Azure.Deployments.Extensibility.TypeGeneration.Attributes.TypeProperty(
                        "Generated count", global::Azure.Bicep.Types.Concrete.ObjectTypePropertyFlags.ReadOnly)]
                    public {{(value == "unsupported" ? "double" : "int")}} GeneratedCount { get; set; }

                    [global::Azure.Deployments.Extensibility.TypeGeneration.Attributes.TypeProperty("Generated details")]
                    public GeneratedDetails? Details { get; set; }
                }

                public sealed class GeneratedDetails
                {
                    public string? Label { get; set; }
                }
                """;
            if (value == "inputs")
            {
                source = "#if TH08_GENERATED\n" + source + "\n#endif";
            }

            output.AddSource("Repository.generated.cs", SourceText.From(source, Encoding.UTF8));
        });
    }
}
