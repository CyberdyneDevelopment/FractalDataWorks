using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.ComponentTypeOptions;

namespace Fdw.Services.Settings.Components.SettingsComponentOptions;

/// <summary>
/// The components over the settings domain.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SettingsComponentBase), typeof(IComponentTypeOption), typeof(SettingsComponents))]
public partial class SettingsComponents : ComponentTypeCollectionBase<SettingsComponentBase>
{
    /// <inheritdoc />
    public override IEnumerable<IComponentTypeOption> Members => All();
}
