namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>An attribute documents or record nodes carry (FDW-3398); its data type says which of an <see cref="IAttributeValue"/>'s values is set.</summary>
public interface IAttributeDefinition : IScopedDefinition
{
    /// <summary>Gets the name shown for the attribute.</summary>
    string Name { get; }

    /// <summary>Gets the data type: Text, Number, Date or Flag.</summary>
    string DataType { get; }

    /// <summary>Gets what carries the attribute: Document or Record.</summary>
    string AppliesTo { get; }
}
