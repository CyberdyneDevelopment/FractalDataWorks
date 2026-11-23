using System;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A parameter of a pipeline that carries a description, seen by the provider that keeps the descriptions of a pipeline and of what it owns (FDW-7323).</summary>
/// <remarks>
/// The pipeline provider knows a pipeline only through <see cref="IPipelineImplementationConfiguration"/>; the parameters are a child of one implementation of it, and this
/// is how it reaches their Id, name and text without naming that implementation.
/// </remarks>
public interface IDescribedPipelineParameter
{
    /// <summary>Gets the parameter's own durable Id, under which its description is kept.</summary>
    Guid Id { get; }

    /// <summary>Gets the parameter's name, unique among the pipeline's parameters.</summary>
    string Name { get; }

    /// <summary>Gets or sets what the parameter is for; empty when the description was not read.</summary>
    string? Description { get; set; }
}
