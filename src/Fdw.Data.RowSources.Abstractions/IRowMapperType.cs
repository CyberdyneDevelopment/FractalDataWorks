using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// TypeOption interface for row mapper types (Pooled, Dynamic).
/// </summary>
/// <remarks>
/// Row mapper types are TypeCollection members. Unlike the existing
/// IEtlRowMapperType which is a ServiceType, these are simpler
/// because mappers don't require per-instance DI - they're created
/// directly by factories using configuration.
/// </remarks>
public interface IRowMapperType : ITypeOption<int, RowMapperTypeBase>
{
    /// <summary>
    /// Gets the estimated allocations per row for this mapper type.
    /// </summary>
    /// <remarks>
    /// 0 = Zero-allocation after warmup (pooled)
    /// 1 = One allocation per row (standard)
    /// Higher = Multiple allocations (complex mappers)
    /// </remarks>
    int EstimatedAllocationsPerRow { get; }

    /// <summary>
    /// Gets whether this mapper supports object pooling.
    /// </summary>
    bool SupportsPooling { get; }

    /// <summary>
    /// Gets whether this mapper supports dynamic/compiled field access.
    /// </summary>
    bool SupportsDynamicAccess { get; }
}
