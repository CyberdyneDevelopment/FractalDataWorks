using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 515: the statement carried NULL for a column that requires a value.
/// The application built the statement without the column, or with a null for it; the same statement fails again.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "NullViolation")]
[ExcludeFromCodeCoverage]
public sealed class NullViolationHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullViolationHandler"/> class.
    /// </summary>
    public NullViolationHandler() : base("NullViolation") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [515];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlStatementRefused(logger, ex, commandText, 515, ex.Message);
}
