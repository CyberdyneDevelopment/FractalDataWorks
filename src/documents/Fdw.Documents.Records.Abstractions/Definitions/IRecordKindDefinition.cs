namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>A kind of record node, such as a project, a site or a case (FDW-3398).</summary>
public interface IRecordKindDefinition : IScopedDefinition
{
    /// <summary>Gets the name shown for the kind.</summary>
    string Name { get; }
}
