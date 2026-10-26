namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>One parameter a TypeCollection option takes, for the console to build its picker from (contract item 4).</summary>
public sealed class TypeCollectionValueParameterDto
{
    /// <summary>Gets or sets the parameter's name, as the pipeline body spells it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter's kind: what the console renders to collect it.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the option needs the parameter.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets what the parameter is for.</summary>
    public string HelpText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a run may supply the setting through a pipeline parameter (FDW-1768): a single-value
    /// setting of a component or step that does not shape the graph. False for every other kind of parameter.
    /// </summary>
    public bool Bindable { get; set; }

    /// <summary>
    /// Gets or sets the <c>DataTypes</c> name of the pipeline parameter a run supplies the setting through (FDW-1777): <c>String</c>,
    /// <c>Int32</c> or <c>Bool</c>. Null when the setting is not bindable. A choice from a list, a data set, a data store, a container, a
    /// pipeline, an expression and a variable name are all kept as text, so all take a <c>String</c>.
    /// </summary>
    public string? BindsTo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the setting is a list a run supplies whole (FDW-1745): <see cref="BindsTo"/> is then the type of
    /// each item, and the pipeline parameter it binds to must be a list parameter. False for a single-value setting and for a setting that is not bindable.
    /// </summary>
    public bool BindsToList { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the setting reads <c>{{Name}}</c> and renders the run's parameters into it (FDW-1768):
    /// the data set, data store, path and container of a source or destination. False for every other kind of parameter.
    /// </summary>
    public bool AcceptsParameterText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a client shows the setting to say what an action does (RUI-547): what a variable is set to, which
    /// pipeline is run, the first line of a script. False for every other kind of parameter.
    /// </summary>
    public bool SummarisesComponent { get; set; }
}
