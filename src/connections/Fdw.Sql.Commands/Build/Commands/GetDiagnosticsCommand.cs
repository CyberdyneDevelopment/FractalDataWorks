using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Build.Commands;

/// <summary>List every TSqlModel validation error / warning, optionally filtered by Severity.</summary>
[TypeOption(typeof(SqlCommands), "GetDiagnostics", RestrictToCurrentCompilation = true)]
public sealed class GetDiagnosticsCommand : SqlCommandBase
{
    public GetDiagnosticsCommand() : base("GetDiagnostics", StandardSqlCommandCategories.Build,
        "List every TSqlModel validation diagnostic. Filter by Severity (Error / Warning / Message). Use as the cheapest 'is this schema valid?' probe.") { }
    public string? Severity { get; set; }
    public string? FilePath { get; set; }
}
