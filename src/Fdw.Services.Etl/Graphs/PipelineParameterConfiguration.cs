using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A parameter a graph pipeline declares at design time (FDW-1549): a typed value whoever starts the run supplies by name, in scope
/// in every step.
/// </summary>
/// <remarks>
/// <para>Cascade child of the graph, in <c>pipe.PipelineParameter</c>.</para>
/// <para>
/// Why no default value: an optional parameter a run leaves blank is absent, and a step that reads it fails. Whoever designs the
/// pipeline decides what each parameter is; the framework names none.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineParameterConfiguration : IGenericConfiguration, IPipelineRunParameter
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the parameter's name, unique among the graph's parameters and variables.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>DataTypes</c> option name the supplied value is converted to.</summary>
    [ValuesFrom(typeof(DataTypes))]
    public string ParameterType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether a run must supply the parameter.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets a value indicating whether no step may change the parameter once the run has started.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter holds a list (FDW-1745): <see cref="ParameterType"/> is then the type of
    /// each item, and a run supplies a JSON array of them.
    /// </summary>
    public bool IsList { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value a run is given for the parameter may be shown in the run's record (FDW-1756). The
    /// author opts a parameter in by stating true; leaving it out is false, and a value that is not shown is recorded only as given.
    /// </summary>
    public bool ShowsValue { get; set; }

    /// <summary>
    /// Gets or sets the value, as text of the parameter's own type, that a run which does not give the parameter uses (FDW-1975); null when the
    /// parameter has no default. The designer writes it; nothing in the code picks a value. A required or list parameter has none.
    /// </summary>
    public string? DefaultText { get; set; }

    /// <summary>Gets or sets what the parameter is for, shown to whoever starts or schedules the run.</summary>
    public string? Description { get; set; }
}
