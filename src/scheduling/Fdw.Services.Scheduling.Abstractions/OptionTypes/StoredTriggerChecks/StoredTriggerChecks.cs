using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// The checks a stored trigger's own validation makes of its configuration: one for each of the trigger types <c>Cron</c>,
/// <c>Interval</c>, <c>Once</c> and <c>Manual</c>.
/// </summary>
/// <remarks>A trigger is checked by the option named for its trigger type, matched exactly through <c>ByName</c>; a type that matches none gets <c>NotFound</c>, and the trigger is refused as of an unknown type.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(StoredTriggerCheckBase), typeof(IStoredTriggerCheck), typeof(StoredTriggerChecks))]
public abstract partial class StoredTriggerChecks : TypeCollectionBase<StoredTriggerCheckBase, IStoredTriggerCheck>
{
}
