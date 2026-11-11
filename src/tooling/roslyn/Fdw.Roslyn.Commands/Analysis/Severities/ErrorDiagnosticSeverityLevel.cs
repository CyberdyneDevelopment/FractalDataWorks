using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>Errors only.</summary>
[TypeOption(typeof(DiagnosticSeverityLevels), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorDiagnosticSeverityLevel : DiagnosticSeverityLevelBase
{
    /// <summary>Initializes a new instance of the <see cref="ErrorDiagnosticSeverityLevel"/> class.</summary>
    public ErrorDiagnosticSeverityLevel() : base(4, "Error") { }

    /// <inheritdoc />
    public override DiagnosticSeverity Severity => DiagnosticSeverity.Error;
}
