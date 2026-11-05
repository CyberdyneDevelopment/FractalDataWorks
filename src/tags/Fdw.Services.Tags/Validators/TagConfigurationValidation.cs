using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Tags.Configuration;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tags.Validators;

/// <summary>States that <see cref="TagImplementationConfiguration"/> is checked by <see cref="TagConfigurationValidator"/> when it is written (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "TagImplementationConfiguration")]
public sealed class TagConfigurationValidation : ValidatedConfiguration<TagImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TagConfigurationValidation"/> class.</summary>
    public TagConfigurationValidation()
        : base("TagImplementationConfiguration", new TagConfigurationValidator())
    {
    }
}
