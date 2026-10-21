using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The result codes of anticipated document failures: what a caller returns instead of catching a storage exception (FDW-1500).</summary>
[TypeCollection(typeof(DocumentResultCodeBase), typeof(IResultCode), typeof(DocumentResultCodes))]
public abstract partial class DocumentResultCodes : TypeCollectionBase<DocumentResultCodeBase, IResultCode>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
