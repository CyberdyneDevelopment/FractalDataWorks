using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Operations.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Operations.Data;

namespace Fdw.Operations.Execution;

/// <summary>
/// Implementation of <see cref="IExecutionItem"/> backed by <see cref="ExecutionItem"/> POCO.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemRecord : IExecutionItem
{
    private readonly ExecutionItem _data;
    private IReadOnlyDictionary<string, object?>? _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemRecord"/> class.
    /// </summary>
    /// <param name="data">The underlying data.</param>
    public ExecutionItemRecord(ExecutionItem data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <inheritdoc />
    public Guid Id => _data.Id;

    /// <inheritdoc />
    public Guid? ParentId => _data.ParentExecutionItemId;

    /// <inheritdoc />
    public Guid RootId => _data.RootExecutionItemId;

    /// <inheritdoc />
    public IExecutionItemType ItemType => ExecutionItemTypes.ByName(_data.ItemType);

    /// <inheritdoc />
    public IExecutionStateType State => ExecutionStateTypes.ByName(_data.State);

    /// <inheritdoc />
    public string Name => _data.Name;

    /// <inheritdoc />
    public string? CorrelationId => _data.CorrelationId;

    /// <inheritdoc />
    public string? TriggerSource => _data.TriggerSource;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt => _data.CreatedAt;

    /// <inheritdoc />
    public DateTimeOffset? StartedAt => _data.StartedAt;

    /// <inheritdoc />
    public DateTimeOffset? CompletedAt => _data.CompletedAt;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Parameters
    {
        get
        {
            if (_parameters != null)
            {
                return _parameters;
            }

            if (string.IsNullOrEmpty(_data.Parameters))
            {
                _parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
                return _parameters;
            }

            try
            {
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, object?>>(_data.Parameters);
                _parameters = deserialized != null
                    ? new Dictionary<string, object?>(deserialized, StringComparer.Ordinal)
                    : new Dictionary<string, object?>(StringComparer.Ordinal);
            }
            catch (JsonException ex)
            {
                // Why: a data record property getter must not throw on corrupt stored JSON; an empty
                // dictionary is returned to keep callers functional. No ILogger is available here
                // (data record context).
                _ = ex;
                _parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
            }

            return _parameters;
        }
    }

    /// <inheritdoc />
    public string? ResultCode => _data.ResultCode;

    /// <inheritdoc />
    public string? ResultMessage => _data.ResultMessage;

    /// <summary>
    /// Gets the underlying POCO data.
    /// </summary>
    internal ExecutionItem Data => _data;

    /// <summary>
    /// Creates an <see cref="ExecutionItem"/> POCO from parameters.
    /// </summary>
    public static ExecutionItem CreatePoco(
        IExecutionItemType itemType,
        string name,
        Guid? parentId,
        Guid rootId,
        string? correlationId,
        string? triggerSource,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        string? parametersJson = null;
        if (parameters != null && parameters.Count > 0)
        {
            parametersJson = JsonSerializer.Serialize(parameters);
        }

        // Why CreateVersion7: app-minted sequential id, matching the ExecutionItem POCO default.
        return new ExecutionItem
        {
            Id = Guid.CreateVersion7(),
            ParentExecutionItemId = parentId,
            RootExecutionItemId = rootId,
            ItemType = itemType.Name,
            Name = name,
            State = ExecutionStateTypes.Scheduled.Name,
            CorrelationId = correlationId,
            TriggerSource = triggerSource,
            Parameters = parametersJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
