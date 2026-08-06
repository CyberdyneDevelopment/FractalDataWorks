using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Identify table columns referenced by WHERE/JOIN predicates that lack indexes.</summary>
[TypeOption(typeof(SqlCommands), "AnalyzeIndexCoverage", RestrictToCurrentCompilation = true)]
public sealed class AnalyzeIndexCoverageCommand : SqlCommandBase
{
    public AnalyzeIndexCoverageCommand()
        : base("AnalyzeIndexCoverage", StandardSqlCommandCategories.Analysis,
               "Cross-reference WHERE/JOIN predicate columns against existing indexes to surface candidate missing indexes.") { }
}
