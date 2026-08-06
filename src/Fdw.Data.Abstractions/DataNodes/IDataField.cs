namespace Fdw.Data.Abstractions;

/// <summary>
/// A named leaf field within an <see cref="IDataContainer"/> that carries type, nullability, and
/// binding information. A field is an <see cref="IDataNode"/> with no children.
/// </summary>
/// <remarks>
/// A field progresses through state as its metadata is populated:
/// <list type="bullet">
/// <item><description><c>IsDescribed</c> — Name is present (non-empty).</description></item>
/// <item><description><c>IsDefined</c> — <see cref="ExplicitType"/> is set (shape is declared).</description></item>
/// <item><description><c>IsBound</c> — <see cref="Binding"/> is set (field can participate in a query).</description></item>
/// </list>
/// Query generation uses only fields where <c>IsDefined &amp;&amp; IsBound</c>.
/// <para>
/// As a leaf <see cref="IDataNode"/>, <see cref="IDataNode.Nodes"/> is always empty and
/// <see cref="IDataNode.Node(string)"/> always fails.
/// </para>
/// <para>
/// The derived members <c>ResolvedType</c>, <c>IsDescribed</c>, <c>IsDefined</c>, and <c>IsBound</c>
/// are interface members without defaults (netstandard2.0 does not support default interface
/// implementations). Concrete implementations should compute them from the primary properties.
/// Extension methods on <c>IDataField</c> are provided in <see cref="DataFieldExtensions"/>
/// for callers that cannot rely on implementation-specific overrides.
/// </para>
/// </remarks>
public interface IDataField : IDataNode
{
    // Name and Description are inherited from IDataNode.

    /// <summary>
    /// Gets the explicitly declared abstract type for this field, if any.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the field is Described-only (no type definition yet).
    /// When <see cref="Binding"/> is present, the binding-derived type takes precedence
    /// over <see cref="ExplicitType"/>. Use <see cref="DataFieldExtensions.ResolvedType"/>
    /// to obtain the effective type.
    /// </remarks>
    IDataType? ExplicitType { get; }

    /// <summary>
    /// Gets the binding that connects this field to a source node, if any.
    /// </summary>
    IFieldBinding? Binding { get; }

    /// <summary>
    /// Gets the zero-based ordinal position of this field within its parent node.
    /// </summary>
    int Ordinal { get; }

    /// <summary>
    /// Gets a value indicating whether this field accepts <see langword="null"/> values.
    /// </summary>
    bool IsNullable { get; }
}
