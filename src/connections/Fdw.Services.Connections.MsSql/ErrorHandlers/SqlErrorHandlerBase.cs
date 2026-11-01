using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.ErrorHandlers;

/// <summary>
/// Base class for SQL error handlers using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class SqlErrorHandlerBase : TypeOptionBase<SqlErrorHandlerBase>, ISqlErrorHandler
{
    /// <summary>
    /// Initializes a new instance for the generated NotFound sentinel.
    /// </summary>
    protected SqlErrorHandlerBase() : base(0, "NotFound")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlErrorHandlerBase"/> class.
    /// </summary>
    /// <param name="name">The name of this handler.</param>
    protected SqlErrorHandlerBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IReadOnlyList<int> SqlErrorNumbers { get; }

    /// <inheritdoc />
    public abstract bool IsRetryable { get; }

    /// <inheritdoc />
    public abstract IGenericMessage CreateFailureMessage(ILogger logger, Exception ex, string commandText);

    /// <inheritdoc />
    /// <remarks>A handler that maps its error says all there is to say in its one message.</remarks>
    public virtual IReadOnlyList<IGenericMessage> CreateFailureMessages(ILogger logger, Exception ex, string commandText)
        => [CreateFailureMessage(logger, ex, commandText)];
}
