namespace Fdw.Data.Abstractions;

/// <summary>
/// A field binding that maps the owning <see cref="IDataField"/> directly to a field
/// on a single source node — no computation or transformation.
/// </summary>
/// <remarks>
/// Used for both Single-composition DataSets (trivial passthrough) and Join/Union DataSets
/// where each output field binds 1:1 to one field from one source.
/// </remarks>
public interface IDirectFieldBinding : IFieldBinding
{
    /// <summary>
    /// Gets the source node that provides the field value.
    /// </summary>
    IDataNode SourceNode { get; }

    /// <summary>
    /// Gets the specific field on <see cref="SourceNode"/> that is bound to.
    /// </summary>
    IDataField SourceField { get; }
}
