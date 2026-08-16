using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Abstract base class for all data commands (non-generic).
/// This base class is used by TypeCollection source generators.
/// </summary>
/// <remarks>
/// <para>
/// Provides common implementation for IDataCommand interface.
/// Use the generic variants (<see cref="DataCommandBase{TResult}"/> or <see cref="DataCommandBase{TResult, TInput}"/>)
/// for actual command implementations.
/// </para>
/// <para>
/// Properties must be set in constructor for TypeCollection source generators to read them.
/// </para>
/// </remarks>
public abstract class DataCommandBase : IDataCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCommandBase"/> class.
    /// </summary>
    /// <param name="commandType">Name of the command type.</param>
    /// <param name="category">The command category (defaults to "Data").</param>
    protected DataCommandBase(string commandType, string category = "Data")
    {
        if (commandType is null)
        {
            // Why: reported defect (see logging-pass report) — this constructor throws instead of
            // returning IGenericResult. Logged here per scope; the throw below is left in place.
            DataCommandBaseLog.CommandTypeMissing(NullLogger<DataCommandBase>.Instance);
        }

        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        Category = category ?? "Data";
        Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        DataCommandBaseLog.CommandCreated(NullLogger<DataCommandBase>.Instance, CommandId, CommandType, Category);
    }

    /// <summary>
    /// Gets the unique identifier for this command instance (implements IGenericCommand).
    /// </summary>
    public Guid CommandId { get; }

    /// <summary>
    /// Gets the timestamp when this command was created (implements IGenericCommand).
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the command type name (implements IGenericCommand).
    /// </summary>
    public string CommandType { get; }

    /// <summary>
    /// Gets the command category (implements IGenericCommand).
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the metadata for this command.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; }
}

/// <summary>
/// Abstract base class for data commands with typed result.
/// </summary>
/// <typeparam name="TResult">The type of result this command returns.</typeparam>
/// <remarks>
/// Use this base class for commands that return a specific type but don't require input data.
/// Examples: QueryCommand&lt;T&gt;, DeleteCommand (returns int).
/// </remarks>
public abstract class DataCommandBase<TResult> : DataCommandBase, IDataCommand<TResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCommandBase{TResult}"/> class.
    /// </summary>
    /// <param name="commandType">Name of the command type.</param>
    protected DataCommandBase(string commandType)
        : base(commandType)
    {
    }
}
/// <summary>
/// Abstract base class for data commands with typed input and typed result.
/// </summary>
/// <typeparam name="TResult">The type of result this command returns.</typeparam>
/// <typeparam name="TInput">The type of input data this command requires.</typeparam>
/// <remarks>
/// Use this base class for commands that require input data and return a specific type.
/// Examples: InsertCommand&lt;T&gt;, UpdateCommand&lt;T&gt;, BulkInsertCommand&lt;T&gt;.
/// </remarks>
public abstract class DataCommandBase<TResult, TInput> : DataCommandBase<TResult>, IDataCommand<TResult, TInput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataCommandBase{TResult, TInput}"/> class.
    /// </summary>
    /// <param name="commandType">Name of the command type.</param>
    /// <param name="data">The input data for this command.</param>
    protected DataCommandBase(string commandType, TInput data)
        : base(commandType)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the input data for this command.
    /// </summary>
    public TInput Data { get; }

    /// <inheritdoc/>
    object? IDataCommandWithInput.InputData => Data;
}
