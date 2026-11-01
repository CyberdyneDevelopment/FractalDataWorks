using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>
/// A row tool in the toolbox that is backed by one stage kind: Filter, Calculate, Sort, Aggregate and the rest.
/// </summary>
/// <remarks>
/// Why the settings are the stage kind's own members: the tool is the stage kind, placed in a graph. The kind already says
/// what it takes (a filter takes its condition, a sort takes its fields), so the tool lists exactly those and adds only what a
/// graph needs of any row tool: a batch size when the kind must hold every row before it answers, and the name of an error
/// output for rows the kind rejects. One place defines what each kind takes, and a kind added later shows up here with no
/// second list to keep.
/// </remarks>
public abstract class StageToolComponentTypeBase : ComponentTypeBase
{
    /// <summary>The name of the node the tool reads rows from.</summary>
    public const string InputName = "input";

    /// <summary>The name of the node the tool writes the rows it keeps to.</summary>
    public const string OutputName = "output";

    /// <summary>The setting that gives how many rows go in a batch when the tool sends a result it held back.</summary>
    public const string BatchSizeSetting = "batchSize";

    /// <summary>The setting that names the error output.</summary>
    public const string ErrorOutputSetting = "errorOutput";

    private static readonly FlowNodeSpec[] InputNodes =
    [
        new FlowNodeSpec(InputName, false, 1, 1, false, "The rows the tool works on."),
    ];

    /// <summary>Initializes a new instance of the <see cref="StageToolComponentTypeBase"/> class.</summary>
    /// <param name="stage">The stage kind the tool is.</param>
    /// <param name="category">What the tool does, by which the toolbox groups it: Filter, Transform, Calculate or Validate.</param>
    /// <param name="outputHelp">What the output node carries, in a sentence.</param>
    /// <param name="mayHoldEveryRow">Whether the tool holds every row for some settings only (a Compute whose transformation works on whole tables); the batch size is then declared but not required.</param>
    protected StageToolComponentTypeBase(TransformTypeBase stage, string category, string outputHelp, bool mayHoldEveryRow = false)
        : this(stage.Name, stage.DisplayName, stage, category, outputHelp, [], mayHoldEveryRow)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StageToolComponentTypeBase"/> class for a tool whose name is not its stage kind's.</summary>
    /// <param name="name">The tool's name.</param>
    /// <param name="displayName">The name the toolbox shows.</param>
    /// <param name="stage">The stage kind the tool is.</param>
    /// <param name="category">What the tool does, by which the toolbox groups it.</param>
    /// <param name="outputHelp">What the output node carries, in a sentence.</param>
    /// <param name="nestedMembers">The names of stage members that are stated inside another member (the transformations of a field mapping), not on the tool.</param>
    /// <param name="mayHoldEveryRow">Whether the tool holds every row for some settings only; the batch size is then declared but not required.</param>
    protected StageToolComponentTypeBase(
        string name, string displayName, TransformTypeBase stage, string category, string outputHelp, IReadOnlyCollection<string> nestedMembers, bool mayHoldEveryRow = false)
        : base(name, displayName, stage.Description, category, InputNodes, OutputNodes(outputHelp), SettingsOf(stage, mayHoldEveryRow, nestedMembers))
    {
        StageKind = stage.Name;
        TakesBatchSize = stage.RequiresCompleteInput || mayHoldEveryRow;
        MemberNames = [.. stage.Members.Select(member => member.Name).Where(member => !nestedMembers.Contains(member, StringComparer.OrdinalIgnoreCase))];
    }

    /// <summary>Gets the name of the stage kind the tool runs.</summary>
    public string StageKind { get; }

    /// <summary>Gets the names of the settings that are members of the stage; the rest (batch size, error output) belong to the component.</summary>
    public IReadOnlyList<string> MemberNames { get; }

    /// <summary>Gets whether the tool declares a batch size: only a kind that holds every row before it answers needs one.</summary>
    public bool TakesBatchSize { get; }

    private static FlowNodeSpec[] OutputNodes(string outputHelp) =>
    [
        new FlowNodeSpec(OutputName, false, 1, 1, false, outputHelp),
        new FlowNodeSpec("error", true, 0, 1, true, "Rows the tool rejected, with the tool and the reason; named by the errorOutput setting.", ErrorOutputSetting),
    ];

    private static List<StageMember> SettingsOf(TransformTypeBase stage, bool mayHoldEveryRow, IReadOnlyCollection<string> nestedMembers)
    {
        var settings = new List<StageMember>(stage.Members.Where(member => !nestedMembers.Contains(member.Name, StringComparer.OrdinalIgnoreCase)));
        if (stage.RequiresCompleteInput)
        {
            settings.Add(new StageMember(BatchSizeSetting, "Number", true, "How many rows go in a batch when the tool sends its result; at least one."));
        }
        else if (mayHoldEveryRow)
        {
            settings.Add(new StageMember(BatchSizeSetting, "Number", false, "How many rows go in a batch when the tool sends a result it held back; at least one. Required when the tool's transformation works on whole tables."));
        }

        settings.Add(new StageMember(ErrorOutputSetting, "Text", false, "The name of the error output; absent, a tool that rejects rows is refused.", ShapesGraph: true));
        return settings;
    }
}
