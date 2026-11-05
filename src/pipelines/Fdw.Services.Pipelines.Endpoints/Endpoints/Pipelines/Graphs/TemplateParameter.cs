using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// A parameter a template declares, in the shape the engine's reader of run parameter values takes (FDW-3379), so a template fill reads a value as a run would.
/// </summary>
/// <param name="Declared">The parameter as the template's payload states it.</param>
internal sealed class TemplateParameter(GraphParameterPayload Declared) : IPipelineRunParameter
{
    /// <inheritdoc />
    public string Name => Declared.Name;

    /// <inheritdoc />
    public string ParameterType => Declared.DataType;

    /// <inheritdoc />
    public bool IsRequired => Declared.IsRequired;

    /// <inheritdoc />
    public bool IsList => Declared.IsList;

    /// <inheritdoc />
    public bool ShowsValue => Declared.ShowsValue;

    /// <inheritdoc />
    public string? DefaultText => Declared.DefaultValue;

    /// <inheritdoc />
    public string? Description => Declared.Description;
}
