using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>TypeCollection for activity types.</summary>
[TypeCollection(typeof(ActivityTypeBase), typeof(IActivityType), typeof(ActivityTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ActivityTypes : TypeCollectionBase<ActivityTypeBase, IActivityType> { }
