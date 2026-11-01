using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Analysis.Commands;

/// <summary>Aggregate workspace statistics: script count, object counts, line counts.</summary>
[TypeOption(typeof(SqlCommands), "GetStatistics", RestrictToCurrentCompilation = true)]
public sealed class GetStatisticsCommand : SqlCommandBase
{
    public GetStatisticsCommand()
        : base("GetStatistics", StandardSqlCommandCategories.Analysis,
               "Aggregate workspace statistics: scripts, total lines, object counts by kind, average proc length.") { }
}
