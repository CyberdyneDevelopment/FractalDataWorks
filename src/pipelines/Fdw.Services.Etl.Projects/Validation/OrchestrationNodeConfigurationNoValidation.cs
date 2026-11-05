using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Projects.Validation;

/// <summary>States that <see cref="OrchestrationNodeImplementationConfiguration"/> needs no validator here, and why (FDW-3541).</summary>
/// <remarks>
/// The configuration abstractions project targets netstandard2.0 and cannot reference Fdw.Validation, so the statement is made here, in the
/// project that implements the orchestration node writer.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "OrchestrationNodeImplementationConfiguration")]
public sealed class OrchestrationNodeConfigurationNoValidation : UnvalidatedConfiguration<OrchestrationNodeImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeConfigurationNoValidation"/> class.</summary>
    public OrchestrationNodeConfigurationNoValidation()
        : base(
            "OrchestrationNodeImplementationConfiguration",
            "OrchestrationNodeConfigurationValidator needs the server policy defaults from the container, so OrchestrationNodeConfigurationWriter runs it before the provider is called; this statement covers the provider's own write.")
    {
    }
}
