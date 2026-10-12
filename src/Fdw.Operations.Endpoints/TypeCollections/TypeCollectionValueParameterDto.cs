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
}
