namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A run parameter a pipeline declares (an entry of <c>parameters</c>); it has no default value.</summary>
public sealed class PipelineParameterDeclaration
{
    /// <summary>Gets or sets the name, referenced as {{name}} in a filter expression.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type name, from the DataTypes type collection (Int32, Int64, Decimal, Double, Float, Bool, String, Guid, Date, DateTime, DateTimeOffset, Time, Int16, Byte; Binary is refused).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a run must supply the parameter.</summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter holds a list (FDW-1745): <see cref="Type"/> is then the type of each
    /// item, and a run supplies a JSON array of them as its value.
    /// </summary>
    public bool IsList { get; set; }

    /// <summary>Gets or sets a value indicating whether the value a run is given may be shown in the run's record; the author opts in by stating true (FDW-1756).</summary>
    public bool ShowsValue { get; set; }

    /// <summary>Gets or sets what the parameter is for, or <see langword="null"/> when it is not described.</summary>
    public string? Description { get; set; }
}
