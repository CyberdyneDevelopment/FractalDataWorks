using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server errors 2601 and 2627: a unique index or constraint refused a duplicate key.
/// The row already exists, and sending the same statement again fails the same way.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "UniqueViolation")]
[ExcludeFromCodeCoverage]
public sealed class UniqueViolationHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueViolationHandler"/> class.
    /// </summary>
    public UniqueViolationHandler() : base("UniqueViolation") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [2601, 2627];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlUniqueViolation(logger, ex, commandText);
}
