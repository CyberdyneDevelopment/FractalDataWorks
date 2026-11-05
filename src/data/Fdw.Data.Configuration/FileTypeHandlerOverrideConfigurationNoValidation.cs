using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections;

/// <summary>States that <see cref="FileTypeHandlerOverrideConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "FileTypeHandlerOverrideConfiguration")]
public sealed class FileTypeHandlerOverrideConfigurationNoValidation : UnvalidatedConfiguration<FileTypeHandlerOverrideConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FileTypeHandlerOverrideConfigurationNoValidation"/> class.</summary>
    public FileTypeHandlerOverrideConfigurationNoValidation()
        : base("FileTypeHandlerOverrideConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
