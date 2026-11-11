using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>Warnings and errors.</summary>
[TypeOption(typeof(DiagnosticSeverityLevels), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningDiagnosticSeverityLevel : DiagnosticSeverityLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="WarningDiagnosticSeverityLevel"/> class.</summary>
    public WarningDiagnosticSeverityLevel() : base(3, "Warning") { }

    /// <inheritdoc />
    public override DiagnosticSeverity Severity => DiagnosticSeverity.Warning;
}
