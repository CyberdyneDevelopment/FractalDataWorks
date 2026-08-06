using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 1205: deadlock victim.
/// The transaction was chosen as a deadlock victim and rolled back.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "Deadlock")]
[ExcludeFromCodeCoverage]
public sealed class DeadlockHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeadlockHandler"/> class.
    /// </summary>
    public DeadlockHandler() : base(5, "Deadlock") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [1205];

    /// <inheritdoc />
    public override bool IsRetryable => true;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlDeadlock(logger, ex, commandText);
}
