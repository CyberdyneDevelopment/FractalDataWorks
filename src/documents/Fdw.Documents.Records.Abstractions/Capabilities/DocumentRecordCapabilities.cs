using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Capabilities;

/// <summary>The capabilities a DocumentRecord connection may declare (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DocumentRecordCapabilityBase), typeof(IDocumentRecordCapability), typeof(DocumentRecordCapabilities))]
public abstract partial class DocumentRecordCapabilities : TypeCollectionBase<DocumentRecordCapabilityBase, IDocumentRecordCapability>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
