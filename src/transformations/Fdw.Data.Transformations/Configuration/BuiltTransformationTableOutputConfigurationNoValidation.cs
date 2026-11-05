using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="BuiltTransformationTableOutputConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "BuiltTransformationTableOutputConfiguration")]
public sealed class BuiltTransformationTableOutputConfigurationNoValidation : UnvalidatedConfiguration<BuiltTransformationTableOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableOutputConfigurationNoValidation"/> class.</summary>
    public BuiltTransformationTableOutputConfigurationNoValidation()
        : base("BuiltTransformationTableOutputConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
