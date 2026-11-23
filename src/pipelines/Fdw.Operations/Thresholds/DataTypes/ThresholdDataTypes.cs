using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// The data types a threshold's stored value text can state: <c>Int64</c>, <c>Double</c>, <c>Boolean</c>, <c>TimeSpan</c>,
/// <c>DateTimeOffset</c> and <c>String</c>.
/// </summary>
/// <remarks>
/// A stored row names its data type by the option's name, matched exactly through <c>ByName</c>; a name that matches none gets
/// <c>NotFound</c>, which reads no text and shows every text as it is.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ThresholdDataTypeBase), typeof(IThresholdDataType), typeof(ThresholdDataTypes))]
public abstract partial class ThresholdDataTypes : TypeCollectionBase<ThresholdDataTypeBase, IThresholdDataType>
{
}
