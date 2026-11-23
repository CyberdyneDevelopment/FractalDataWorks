using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Descriptions.Configuration;

/// <summary>States that <see cref="DescriptionImplementationConfiguration"/> has no validator of its own, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DescriptionImplementationConfiguration")]
public sealed class DescriptionImplementationConfigurationNoValidation : UnvalidatedConfiguration<DescriptionImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionImplementationConfigurationNoValidation"/> class.</summary>
    public DescriptionImplementationConfigurationNoValidation()
        : base("DescriptionImplementationConfiguration", "The descriptions service is the only writer and checks every key, name and text before it prepares the record (DescriptionService.Prepare), and a table CHECK refuses an empty text; no second validator is written for the same rules.")
    {
    }
}
