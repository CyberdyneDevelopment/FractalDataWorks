namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// A run parameter a pipeline declares (FDW-1549): the name a trigger or a schedule supplies a value under, the data type the value is
/// converted to, and whether a run must supply it. A graph pipeline declares its own in its parameters table; run start and
/// the parameter checks read them through this.
/// </summary>
public interface IPipelineRunParameter
{
    /// <summary>Gets the parameter's name, unique among the pipeline's parameters.</summary>
    string Name { get; }

    /// <summary>Gets the <c>DataTypes</c> option name the supplied value is converted to.</summary>
    string ParameterType { get; }

    /// <summary>Gets a value indicating whether a run must supply the parameter.</summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the parameter holds a list (FDW-1745): <see cref="ParameterType"/> is then the type of each
    /// item, and the value a run supplies is a JSON array of them.
    /// </summary>
    bool IsList { get; }

    /// <summary>
    /// Gets a value indicating whether the value a run is given for the parameter may be shown in the run's record (FDW-1756): its author
    /// opted it in. A parameter that does not show its value is recorded as given only.
    /// </summary>
    bool ShowsValue { get; }

    /// <summary>
    /// Gets the value, as text of the parameter's own type, that a run which does not give the parameter uses (FDW-1975); null when the
    /// parameter has no default.
    /// </summary>
    string? DefaultText { get; }

    /// <summary>Gets what the parameter is for, as the designer wrote it; null when nothing was written.</summary>
    string? Description { get; }
}
