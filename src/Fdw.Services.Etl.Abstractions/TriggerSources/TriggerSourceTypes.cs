using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.TriggerSources;

/// <summary>
/// The trigger sources an ETL run can be attributed to.
/// </summary>
/// <remarks>
/// Declared here rather than in a host or a reference package: the collection is part of the ETL
/// contract, and the requests that carry a trigger source live in this assembly. Members are supplied
/// by whichever package ships them, including downstream ones.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TriggerSourceBase), typeof(ITriggerSource), typeof(TriggerSourceTypes))]
public abstract partial class TriggerSourceTypes : TypeCollectionBase<TriggerSourceBase, ITriggerSource>
{
}
