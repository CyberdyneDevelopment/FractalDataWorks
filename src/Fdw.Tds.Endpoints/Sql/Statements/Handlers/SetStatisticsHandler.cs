using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET STATISTICS IO|TIME|PROFILE|XML: accepted. SQL Server reports the statistics as messages beside the
/// results; this server has none to report, so turning them on adds nothing and changes no result.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "SetStatistics", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetStatisticsHandler : SqlStatementHandlerBase<SetStatisticsStatement>
{
    /// <summary>Initializes a new instance of the <see cref="SetStatisticsHandler"/> class.</summary>
    public SetStatisticsHandler()
        : base("SetStatistics")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetStatisticsStatement statement, SqlExecutionContext context, CancellationToken cancellationToken) => Ran();
}
