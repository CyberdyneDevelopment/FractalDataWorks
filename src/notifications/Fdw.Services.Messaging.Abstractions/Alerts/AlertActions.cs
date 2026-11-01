using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>
/// The actions a person can take on an alert: Acknowledge, Silence, Snooze, Dismiss and Discard (FDW-2420). The reference set is one worked example; a developer
/// adds an action or replaces one by declaring an option of their own.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AlertActionBase), typeof(IAlertAction), typeof(AlertActions))]
public abstract partial class AlertActions : TypeCollectionBase<AlertActionBase, IAlertAction>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
