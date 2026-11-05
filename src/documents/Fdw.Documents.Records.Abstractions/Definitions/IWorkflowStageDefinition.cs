namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>A workflow stage a document of a type passes through (FDW-3398); its <see cref="IStateChange"/> entries name it.</summary>
public interface IWorkflowStageDefinition : IScopedDefinition
{
    /// <summary>Gets the name shown for the stage.</summary>
    string Name { get; }

    /// <summary>Gets the code of the document type the stage belongs to.</summary>
    string DocumentType { get; }

    /// <summary>Gets the stage's place among its document type's stages, first is lowest.</summary>
    int Order { get; }
}
