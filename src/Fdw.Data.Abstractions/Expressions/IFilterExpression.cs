namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for hierarchical filter expressions (WHERE clause representation).
/// </summary>
/// <remarks>
/// <para>
/// Represents universal WHERE clause that works across all data sources.
/// Translators convert to SQL WHERE, OData $filter, file filtering, etc.
/// </para>
/// <para>
/// Uses composite pattern with FilterCondition (leaf) and FilterGroup (composite)
/// to support complex nested filters with proper precedence.
/// </para>
/// </remarks>
public interface IFilterExpression
{
    /// <summary>
    /// Gets the root filter node.
    /// Can be a FilterCondition (single condition) or FilterGroup (complex nested filter).
    /// </summary>
    IFilterNode? Root { get; }
}
