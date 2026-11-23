using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>Base class for the settings a container component states about the target it reads or writes.</summary>
public abstract class TargetSettingBase : TypeOptionBase<TargetSettingBase>, ITargetSetting
{
    /// <summary>Initializes a new instance of the <see cref="TargetSettingBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="word">The word a target kind and a graph name the setting by.</param>
    protected TargetSettingBase(string name, string word)
        : base(name)
    {
        Word = word;
    }

    /// <inheritdoc />
    [TypeLookup("ByWord")]
    public string Word { get; }

    /// <inheritdoc />
    public abstract string? ValueOf(PipelineComponentConfiguration component);
}
