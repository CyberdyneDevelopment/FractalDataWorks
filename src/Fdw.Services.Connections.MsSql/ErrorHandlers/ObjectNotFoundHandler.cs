using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Handles SQL Server error 208: invalid object name.
/// The referenced table, view, or stored procedure does not exist.
/// </summary>
[TypeOption(typeof(SqlErrorHandlers), "ObjectNotFound")]
[ExcludeFromCodeCoverage]
public sealed class ObjectNotFoundHandler : SqlErrorHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectNotFoundHandler"/> class.
    /// </summary>
    public ObjectNotFoundHandler() : base(2, "ObjectNotFound") { }

    /// <inheritdoc />
    public override IReadOnlyList<int> SqlErrorNumbers => [208];

    /// <inheritdoc />
    public override bool IsRetryable => false;

    /// <inheritdoc />
    public override IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText)
        => MsSqlConnectionLogger.SqlObjectNotFound(logger, ex, commandText);
}
