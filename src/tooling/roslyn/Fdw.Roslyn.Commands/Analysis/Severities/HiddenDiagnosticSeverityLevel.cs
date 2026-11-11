using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>Hidden diagnostics and above: every diagnostic.</summary>
[TypeOption(typeof(DiagnosticSeverityLevels), "Hidden")]
[ExcludeFromCodeCoverage]
public sealed class HiddenDiagnosticSeverityLevel : DiagnosticSeverityLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="HiddenDiagnosticSeverityLevel"/> class.</summary>
    public HiddenDiagnosticSeverityLevel() : base(1, "Hidden") { }

    /// <inheritdoc />
    public override DiagnosticSeverity Severity => DiagnosticSeverity.Hidden;
}
