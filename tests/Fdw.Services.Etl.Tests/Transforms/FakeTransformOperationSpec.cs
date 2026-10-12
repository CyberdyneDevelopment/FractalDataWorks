using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Minimal <see cref="ITransformOperationSpec"/> test double for exercising
/// <c>TransformTypeBase.MapSpecToConfiguration</c> dispatch without depending on the
/// web-layer request DTO (<c>CreatePipelineTransformRequest</c>).
/// </summary>
internal sealed class FakeTransformOperationSpec : ITransformOperationSpec, ITransformOrderingSpec, ITransformCheckingSpec, ITransformNotifyingSpec, ITransformUnionSpec
{
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string OperationType { get; set; } = string.Empty;

    /// <inheritdoc />
    public int ExecutionOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyList<IFieldMapping> FieldMappings { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<string> GroupByFields { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<IAggregationSpec> Aggregations { get; set; } = [];

    /// <inheritdoc />
    public ILookupSpec? Lookup { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ICalculationSpec> ComputedColumns { get; set; } = [];

    /// <inheritdoc />
    public string? FilterExpression { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ISortFieldSpec> SortFields { get; set; } = [];

    /// <inheritdoc />
    public string? OrderField { get; set; }

    /// <inheritdoc />
    public string? KeepRule { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ITransformCheckSpec> Checks { get; set; } = [];

    /// <inheritdoc />
    public string? OnFail { get; set; }

    /// <inheritdoc />
    public INotificationSpec? Notification { get; set; }

    /// <inheritdoc />
    public string? UnionSource { get; set; }
}
