using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server errors -1, 2, and 53: connection failed.
/// The SQL Server instance is unreachable due to network or service issues.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "ConnectionFailed")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionFailedHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedHandler"/> class.
    /// </summary>
    public ConnectionFailedHandler() : base(4, "ConnectionFailed") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [-1, 2, 53];

    /// <inheritdoc />
    public override bool IsRetryable => true;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlServerUnreachable(logger, ex, commandText);
}
