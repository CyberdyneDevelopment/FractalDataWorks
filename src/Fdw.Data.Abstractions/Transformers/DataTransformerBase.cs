using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data transformer implementations (non-generic marker base).
/// </summary>
public abstract class DataTransformerBase : TypeOptionBase<int, DataTransformerBase>, IDataTransformer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTransformerBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this transformer type.</param>
    /// <param name="name">The name of this transformer.</param>
    /// <param name="category">The category for this transformer (defaults to "Transformer").</param>
    protected DataTransformerBase(int id, string name, string? category = "Transformer") : base(id, name, category)
    { }

    /// <summary>
    /// Gets the transformer name.
    /// </summary>
    public abstract string TransformerName { get; }
}
