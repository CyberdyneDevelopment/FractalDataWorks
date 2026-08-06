using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error -2: query timeout.
/// The command exceeded the configured timeout period.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "QueryTimeout")]
[ExcludeFromCodeCoverage]
public sealed class QueryTimeoutHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTimeoutHandler"/> class.
    /// </summary>
    public QueryTimeoutHandler() : base(6, "QueryTimeout") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [-2];

    /// <inheritdoc />
    public override bool IsRetryable => true;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlQueryTimeout(logger, ex, commandText);
}
