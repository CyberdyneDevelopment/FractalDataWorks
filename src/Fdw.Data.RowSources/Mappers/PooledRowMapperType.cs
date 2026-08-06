using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Mappers;

/// <summary>
/// TypeOption for pooled row mappers.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "TypeOption - no logic to test")]
[TypeOption(typeof(RowMapperTypes), "Pooled", RestrictToCurrentCompilation = true)]
public sealed class PooledRowMapperType : RowMapperTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PooledRowMapperType"/> class.
    /// </summary>
    public PooledRowMapperType() : base(1, "Pooled")
    {
    }

    /// <inheritdoc />
    public override int EstimatedAllocationsPerRow => 0;

    /// <inheritdoc />
    public override bool SupportsPooling => true;

    /// <inheritdoc />
    public override bool SupportsDynamicAccess => false;
}
