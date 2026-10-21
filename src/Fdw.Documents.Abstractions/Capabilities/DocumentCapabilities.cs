using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>The capabilities a document connection may declare (FDW-1499).</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DocumentCapabilityBase), typeof(IDocumentCapability), typeof(DocumentCapabilities))]
public abstract partial class DocumentCapabilities : TypeCollectionBase<DocumentCapabilityBase, IDocumentCapability>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
