using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for every vectorized command (non-generic), the type the <see cref="VectorizedDataCommands"/> collection
/// holds.
/// </summary>
public abstract class VectorizedDataCommandBase : IVectorizedDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorizedDataCommandBase"/> class.
    /// </summary>
    /// <param name="commandType">The command's name, which a vectorized connection finds its translator by.</param>
    protected VectorizedDataCommandBase(string commandType)
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType;
        Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        VectorizedDataCommandBaseLog.CommandCreated(NullLogger<VectorizedDataCommandBase>.Instance, CommandId, CommandType);
    }

    /// <inheritdoc/>
    public Guid CommandId { get; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public string CommandType { get; }

    /// <inheritdoc/>
    public string Category => "Vectorized";

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Base class for a vectorized command that answers with <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public abstract class VectorizedDataCommandBase<TResult> : VectorizedDataCommandBase, IVectorizedDataCommand<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorizedDataCommandBase{TResult}"/> class.
    /// </summary>
    /// <param name="commandType">The command's name, which a vectorized connection finds its translator by.</param>
    protected VectorizedDataCommandBase(string commandType)
        : base(commandType)
    {
    }
}
