namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>A rule that a kind of record node is expected to have a document of a type (FDW-3398); it is what <see cref="IRecordRequirement"/> entries come from.</summary>
public interface IRequirementRuleDefinition : IScopedDefinition
{
    /// <summary>Gets the code of the record kind the rule applies to.</summary>
    string RecordKind { get; }

    /// <summary>Gets the code of the document type expected.</summary>
    string ExpectedDocumentType { get; }
}
