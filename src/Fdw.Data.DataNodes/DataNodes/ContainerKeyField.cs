using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Runtime implementation of <see cref="IContainerKeyField"/>.
/// Constructed from <c>data.DataContainerKeyField</c> rows by the per-transport
/// <c>DataStoreBuilderBase</c> and by <c>MsSqlDataContainerDetailLoader</c> for lazy-loaded containers.
/// </summary>
// Why: public so MsSqlDataContainerDetailLoader in Services.Connections.MsSql can
// construct ContainerKeyField instances.
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ContainerKeyField : IContainerKeyField
{
    /// <inheritdoc />
    public IDataField LocalField { get; }

    /// <inheritdoc />
    public IDataField? ReferencedField { get; }

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <summary>Initializes a new instance of the <see cref="ContainerKeyField"/> class.</summary>
    public ContainerKeyField(IDataField localField, IDataField? referencedField, int ordinal)
    {
        LocalField = localField;
        ReferencedField = referencedField;
        Ordinal = ordinal;
    }
}
