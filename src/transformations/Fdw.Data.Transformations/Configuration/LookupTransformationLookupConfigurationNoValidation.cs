using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="LookupTransformationLookupConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "LookupTransformationLookupConfiguration")]
public sealed class LookupTransformationLookupConfigurationNoValidation : UnvalidatedConfiguration<LookupTransformationLookupConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LookupTransformationLookupConfigurationNoValidation"/> class.</summary>
    public LookupTransformationLookupConfigurationNoValidation()
        : base("LookupTransformationLookupConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
