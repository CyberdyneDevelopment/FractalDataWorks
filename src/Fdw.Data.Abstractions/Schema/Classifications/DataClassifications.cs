using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The data classification levels: Public, Internal, Confidential, Protected and Restricted.
/// </summary>
/// <remarks>
/// The platform defines the levels; <c>data.DataClassification</c> holds one row per level so a
/// <c>ClassificationId</c> column always names a row that exists. A stored id resolves to its level
/// through the generated <c>ById</c>, which answers NotFound for an id no level declares (FDW-938).
/// </remarks>
[TypeCollection(typeof(DataClassificationBase), typeof(IDataClassification), typeof(DataClassifications))]
public abstract partial class DataClassifications : TypeCollectionBase<DataClassificationBase, IDataClassification>
{
}
