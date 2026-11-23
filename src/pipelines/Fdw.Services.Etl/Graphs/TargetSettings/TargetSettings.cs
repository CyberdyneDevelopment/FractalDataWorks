using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The settings a container component states about the target it reads or writes: <c>container</c>, <c>dataSet</c>,
/// <c>physicalPath</c>, <c>physicalName</c>, <c>filesList</c> and <c>splitField</c>.
/// </summary>
/// <remarks>A target kind names a setting by its word, matched exactly through <c>ByWord</c>; a word that matches none gets <c>NotFound</c>, and a kind that names it is a mistake in the kind.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TargetSettingBase), typeof(ITargetSetting), typeof(TargetSettings))]
public abstract partial class TargetSettings : TypeCollectionBase<TargetSettingBase, ITargetSetting>
{
}
