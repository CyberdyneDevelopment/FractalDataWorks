using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Data;

namespace Fdw.Operations.Execution;

/// <summary>
/// Implementation of <see cref="IExecutionEvent"/> backed by <see cref="ExecutionEvent"/> POCO.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionEventRecord : IExecutionEvent
{
    private readonly ExecutionEvent _data;
    private IReadOnlyDictionary<string, object?>? _eventData;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionEventRecord"/> class.
    /// </summary>
    /// <param name="data">The underlying data.</param>
    public ExecutionEventRecord(ExecutionEvent data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <inheritdoc />
    public Guid Id => _data.Id;

    /// <inheritdoc />
    public Guid ExecutionItemId => _data.ExecutionItemId;

    /// <inheritdoc />
    public int SequenceNumber => _data.SequenceNumber;

    /// <inheritdoc />
    public DateTimeOffset Timestamp => _data.Timestamp;

    /// <inheritdoc />
    public string EventType => _data.EventType;

    /// <inheritdoc />
    public string? PreviousState => _data.PreviousState;

    /// <inheritdoc />
    public string? NewState => _data.NewState;

    /// <inheritdoc />
    public string? Message => _data.Message;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Data
    {
        get
        {
            if (_eventData != null)
            {
                return _eventData;
            }

            if (string.IsNullOrEmpty(_data.Data))
            {
                return null;
            }

            try
            {
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, object?>>(_data.Data);
                _eventData = deserialized != null
                    ? new Dictionary<string, object?>(deserialized, StringComparer.Ordinal)
                    : null;
            }
            catch (JsonException ex)
            {
                _ = ex;
                _eventData = null;
            }

            return _eventData;
        }
    }

    /// <inheritdoc />
    public string? Actor => _data.Actor;

    /// <summary>
    /// Gets the underlying POCO data.
    /// </summary>
    internal ExecutionEvent EventData => _data;

    /// <summary>
    /// Creates an <see cref="ExecutionEvent"/> POCO from parameters.
    /// </summary>
    public static ExecutionEvent CreatePoco(
        Guid executionItemId,
        int sequenceNumber,
        string eventType,
        string? previousState,
        string? newState,
        string? message,
        IReadOnlyDictionary<string, object?>? data,
        string? actor)
    {
        string? dataJson = null;
        if (data != null && data.Count > 0)
        {
            dataJson = JsonSerializer.Serialize(data);
        }

        return new ExecutionEvent
        {
            Id = Guid.CreateVersion7(),
            ExecutionItemId = executionItemId,
            SequenceNumber = sequenceNumber,
            EventType = eventType,
            PreviousState = previousState,
            NewState = newState,
            Message = message,
            Data = dataJson,
            Actor = actor,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
