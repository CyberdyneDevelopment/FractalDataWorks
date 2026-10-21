using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Base class for the options of <see cref="ComponentTypes"/>, using the CRTP pattern.
/// </summary>
public abstract class ComponentTypeBase : TypeOptionBase<ComponentTypeBase>, IComponentType
{
    /// <summary>The name of the setting every kind with inputs has: the mapping of each input (FDW-2013).</summary>
    public const string MappingsSetting = "mappings";

    /// <summary>The catalogue kind of <see cref="MappingsSetting"/> (FDW-2026), so a client draws it by what it is and not by its name; Lookup's bring and a SQL source's parameters are plain <c>FieldMaps</c>.</summary>
    public const string MappingsKind = "InputMappings";

    /// <summary>The name of the setting every kind has: the stage the component runs in (FDW-2040).</summary>
    public const string StageSetting = "stage";

    private static readonly StageMember Stage = new(
        StageSetting, "Number", false,
        "The stage this component runs in, a number from 1; absent, it runs first, before every stage. When every component of a stage has finished, the next higher stage runs. A line may join components of different stages, to the same or a later one; the rows wait in memory until the later stage starts.",
        ShapesGraph: true);

    private static readonly StageMember Mappings = new(
        MappingsSetting, MappingsKind, false,
        "For each input, the fields the component sees: each from an incoming field or a constant, and for each a type to convert to (a data type), a default, whether a value is required and a chain of named Transformations; absent, the rows pass through unchanged. Every entry names its input.",
        TakesPairList: true, TakesConversions: true);

    /// <summary>Initializes a new instance for the Empty sentinel: a kind with no nodes and no settings.</summary>
    protected ComponentTypeBase()
        : base("NotFound", "Not found", "Unknown component kind", "ComponentTypes")
    {
        Inputs = [];
        Outputs = [];
        Settings = [];
    }

    /// <summary>Initializes a new instance of the <see cref="ComponentTypeBase"/> class.</summary>
    /// <param name="name">The option name, which a stored component names as its kind.</param>
    /// <param name="displayName">The label to show a person.</param>
    /// <param name="description">What the component does.</param>
    /// <param name="category">Where the console groups it: Source, Transformation, Join, Destination or Control flow.</param>
    /// <param name="inputs">The input nodes.</param>
    /// <param name="outputs">The output nodes.</param>
    /// <param name="settings">The settings the kind takes; a kind with inputs also takes <see cref="MappingsSetting"/>, which it does not list.</param>
    protected ComponentTypeBase(
        string name,
        string displayName,
        string description,
        string category,
        IReadOnlyList<FlowNodeSpec> inputs,
        IReadOnlyList<FlowNodeSpec> outputs,
        IReadOnlyList<StageMember> settings)
        : base(name, displayName, description, category)
    {
        Inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // Why every kind with an input takes it: a mapping is not a step of its own, it is what happens to rows as they arrive at an input.
        Settings = inputs.Count > 0 ? [.. settings, Mappings, Stage] : [.. settings, Stage];
    }

    /// <summary>Initializes a new instance for a kind that is an action or a loop (FDW-2041): it moves no rows, so it has no nodes, and it runs as a step of the flow.</summary>
    /// <param name="name">The option name, which a stored component names as its kind.</param>
    /// <param name="displayName">The label to show a person.</param>
    /// <param name="description">What the component does.</param>
    /// <param name="category">Where the console groups it.</param>
    /// <param name="holdsComponents">Whether it is a loop, which runs the components that name it as their parent.</param>
    /// <param name="settings">The settings the kind takes.</param>
    protected ComponentTypeBase(
        string name,
        string displayName,
        string description,
        string category,
        bool holdsComponents,
        IReadOnlyList<StageMember> settings)
        : this(name, displayName, description, category, [], [], settings)
    {
        IsAction = true;
        HoldsComponents = holdsComponents;
    }

    /// <inheritdoc />
    public bool IsAction { get; }

    /// <inheritdoc />
    public bool HoldsComponents { get; }

    /// <inheritdoc />
    public IReadOnlyList<FlowNodeSpec> Inputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<FlowNodeSpec> Outputs { get; }

    /// <inheritdoc />
    public IReadOnlyList<StageMember> Settings { get; }
}
