using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.DocumentRecords;

/// <summary>
/// The DocumentRecord extensions of the connections (FDW-3673), one for each kind of connection that can keep document records.
/// </summary>
/// <remarks>
/// Like <see cref="ScriptedConnectionTypes"/>: the shared connections do not implement <see cref="IDocumentRecordConnection"/>; an extension
/// package does, around them, and only a host that references one has its options. A host that references none has an empty collection, so
/// no connection there fits the purpose DocumentRecords unless it is a DocumentRecord connection itself.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DocumentRecordConnectionTypeBase), typeof(IDocumentRecordConnectionType), typeof(DocumentRecordConnectionTypes), RestrictToCurrentCompilation = false)]
public abstract partial class DocumentRecordConnectionTypes : TypeCollectionBase<DocumentRecordConnectionTypeBase, IDocumentRecordConnectionType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
