using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>States that <see cref="BuiltTransformationTableInputColumnConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "BuiltTransformationTableInputColumnConfiguration")]
public sealed class BuiltTransformationTableInputColumnConfigurationNoValidation : UnvalidatedConfiguration<BuiltTransformationTableInputColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="BuiltTransformationTableInputColumnConfigurationNoValidation"/> class.</summary>
    public BuiltTransformationTableInputColumnConfigurationNoValidation()
        : base("BuiltTransformationTableInputColumnConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
