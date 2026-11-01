using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 547: a foreign key (or check) constraint refused the statement.
/// A referenced row is missing, or the row is still referenced by another.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "ReferenceViolation")]
[ExcludeFromCodeCoverage]
public sealed class ReferenceViolationHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceViolationHandler"/> class.
    /// </summary>
    public ReferenceViolationHandler() : base("ReferenceViolation") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [547];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlReferenceViolation(logger, ex, commandText);
}
