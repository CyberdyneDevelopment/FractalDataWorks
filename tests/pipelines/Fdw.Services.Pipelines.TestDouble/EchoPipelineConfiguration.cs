using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>The implementation row of the test pipeline type <see cref="EchoPipelineType"/>; it holds nothing a test reads.</summary>
[ExcludeFromCodeCoverage]
public sealed class EchoPipelineConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }
}
