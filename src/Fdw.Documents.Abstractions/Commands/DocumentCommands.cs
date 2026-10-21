using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>The document command family (FDW-1497): what a document connection is asked to do.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DocumentCommandBase), typeof(IDocumentCommand), typeof(DocumentCommands))]
public abstract partial class DocumentCommands : TypeCollectionBase<DocumentCommandBase, IDocumentCommand>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
