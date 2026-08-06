using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 18456: login failed.
/// The database credentials are invalid or the login is disabled.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "LoginFailed")]
[ExcludeFromCodeCoverage]
public sealed class LoginFailedHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginFailedHandler"/> class.
    /// </summary>
    public LoginFailedHandler() : base(3, "LoginFailed") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [18456];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlLoginFailed(logger, ex, commandText);
}
