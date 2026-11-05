namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>A kind of link from a document, such as replaces or supersedes (FDW-3398, FDW-3397).</summary>
public interface ILinkKindDefinition : IScopedDefinition
{
    /// <summary>Gets the name shown for the kind.</summary>
    string Name { get; }
}
