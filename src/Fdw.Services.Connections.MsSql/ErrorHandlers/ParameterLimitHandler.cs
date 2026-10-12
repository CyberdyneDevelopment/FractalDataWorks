using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 8003: the request carried more parameters than the server accepts (2100).
/// The application built an over-sized statement; sending it again fails the same way.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "ParameterLimit")]
[ExcludeFromCodeCoverage]
public sealed class ParameterLimitHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterLimitHandler"/> class.
    /// </summary>
    public ParameterLimitHandler() : base("ParameterLimit") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [8003];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlStatementRefused(logger, ex, commandText, 8003, ex.Message);
}
