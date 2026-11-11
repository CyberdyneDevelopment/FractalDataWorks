using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>Info diagnostics and above.</summary>
[TypeOption(typeof(DiagnosticSeverityLevels), "Info")]
[ExcludeFromCodeCoverage]
public sealed class InfoDiagnosticSeverityLevel : DiagnosticSeverityLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="InfoDiagnosticSeverityLevel"/> class.</summary>
    public InfoDiagnosticSeverityLevel() : base(2, "Info") { }

    /// <inheritdoc />
    public override DiagnosticSeverity Severity => DiagnosticSeverity.Info;
}
