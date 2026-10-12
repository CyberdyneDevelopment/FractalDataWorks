using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for activity severity levels.
/// </summary>
[TypeCollection(typeof(ActivitySeverityBase), typeof(IActivitySeverity), typeof(ActivitySeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class ActivitySeverities : TypeCollectionBase<ActivitySeverityBase, IActivitySeverity> { }
