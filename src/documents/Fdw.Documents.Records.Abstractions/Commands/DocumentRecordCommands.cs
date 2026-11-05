using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>The document record command family (FDW-3673): what a DocumentRecord connection is asked to do.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DocumentRecordCommandBase), typeof(IDocumentRecordCommand), typeof(DocumentRecordCommands))]
public abstract partial class DocumentRecordCommands : TypeCollectionBase<DocumentRecordCommandBase, IDocumentRecordCommand>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
