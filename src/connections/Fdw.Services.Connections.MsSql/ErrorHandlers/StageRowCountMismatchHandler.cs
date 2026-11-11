using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 51956, which the sweep of a staged key set raises (FDW-4956): the load's stage did not hold exactly the rows the load
/// appended, so the batch stopped before it flagged anything. A short stage would make present keys look gone, so it is refused, not retried.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "StageRowCountMismatch")]
[ExcludeFromCodeCoverage]
public sealed class StageRowCountMismatchHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageRowCountMismatchHandler"/> class.
    /// </summary>
    public StageRowCountMismatchHandler() : base("StageRowCountMismatch") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [Fdw.Data.MsSql.MsSqlDeleteTranslator.StageRowCountMismatchError];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlStageRowCountMismatch(logger, ex, ex.Message);
}
