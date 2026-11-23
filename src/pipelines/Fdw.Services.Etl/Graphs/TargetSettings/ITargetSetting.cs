using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting a container component states about the target it reads or writes.</summary>
public interface ITargetSetting : ITypeOption<int, TargetSettingBase>
{
    /// <summary>Gets the word a target kind and a graph name the setting by.</summary>
    string Word { get; }

    /// <summary>Reads the setting from a component.</summary>
    /// <param name="component">The component.</param>
    /// <returns>What the component states, or <c>null</c> when it states nothing.</returns>
    string? ValueOf(PipelineComponentConfiguration component);
}
