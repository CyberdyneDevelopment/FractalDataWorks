using Fdw.Collections;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>One value of <see cref="DiagnosticSeverityLevels"/>: the lowest severity a diagnostics request reports, carrying the Roslyn <see cref="DiagnosticSeverity"/> it stands for.</summary>
public interface IDiagnosticSeverityLevel : ITypeOption<int, DiagnosticSeverityLevelBase>
{
    /// <summary>Gets the Roslyn severity this level stands for.</summary>
    DiagnosticSeverity Severity { get; }
}
