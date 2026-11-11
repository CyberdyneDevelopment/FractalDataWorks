using Fdw.Collections;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Severities;

/// <summary>Base of every <see cref="DiagnosticSeverityLevels"/> option: the lowest severity a diagnostics request reports, carrying the Roslyn <see cref="DiagnosticSeverity"/> it stands for.</summary>
public abstract class DiagnosticSeverityLevelBase : TypeOptionBase<DiagnosticSeverityLevelBase>, IDiagnosticSeverityLevel
{
    /// <summary>Initializes a new instance of the <see cref="DiagnosticSeverityLevelBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as a request states it (matched ignoring case).</param>
    protected DiagnosticSeverityLevelBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract DiagnosticSeverity Severity { get; }
}
