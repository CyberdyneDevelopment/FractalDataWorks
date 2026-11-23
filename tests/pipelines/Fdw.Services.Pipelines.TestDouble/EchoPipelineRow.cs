using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// The implementation row of the test double's pipeline types (FDW-6244): what <see cref="EchoRowProvider"/> reads. No test reads a row; it exists so a
/// test pipeline type registers a row provider under its name, as every pipeline type must.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EchoPipelineRow : IPipelineImplementationConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid? OrgId { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public System.Collections.Generic.IReadOnlyList<Fdw.Services.Pipelines.Abstractions.IDescribedPipelineParameter> DescribedParameters => [];

    /// <inheritdoc />
    public DateTimeOffset CreateDate { get; set; }

    /// <inheritdoc />
    public DateTimeOffset ModifyDate { get; set; }
}
