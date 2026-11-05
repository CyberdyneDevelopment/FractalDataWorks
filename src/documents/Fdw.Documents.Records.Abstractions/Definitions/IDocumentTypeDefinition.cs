namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>A document type an organization keeps documents of (FDW-3398); it also names the retention policy that applies to them.</summary>
public interface IDocumentTypeDefinition : IScopedDefinition
{
    /// <summary>Gets the name shown for the type.</summary>
    string Name { get; }
}
