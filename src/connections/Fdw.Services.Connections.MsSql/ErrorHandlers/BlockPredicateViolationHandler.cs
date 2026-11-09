using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 33504: a block predicate of a row-level security policy refused the write (FDW-4193).
/// The caller's tenant may not write that row. It is a refusal by policy and not a fault of the server, so the failure is in the Forbidden
/// category, and sending the same statement again fails the same way.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "BlockPredicateViolation")]
[ExcludeFromCodeCoverage]
public sealed class BlockPredicateViolationHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockPredicateViolationHandler"/> class.
    /// </summary>
    public BlockPredicateViolationHandler() : base("BlockPredicateViolation") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [33504];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlBlockPredicateViolation(logger, ex, commandText);
}
