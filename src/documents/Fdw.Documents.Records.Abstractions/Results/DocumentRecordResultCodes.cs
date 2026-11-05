using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The result codes of anticipated document record failures: what an implementation of the record seams returns (FDW-3395).</summary>
[TypeCollection(typeof(DocumentResultCodeBase), typeof(IResultCode), typeof(DocumentRecordResultCodes))]
public abstract partial class DocumentRecordResultCodes : TypeCollectionBase<DocumentResultCodeBase, IResultCode>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
