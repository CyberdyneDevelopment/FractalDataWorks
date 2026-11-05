using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>States that the cascade-save test stand-in with no mapper <see cref="RecursiveCascadeSaveTests.TestUnmappedRootConfiguration"/> needs no validator (FDW-3541).</summary>
[TypeOption(typeof(ConfigurationValidators), "TestUnmappedRootConfiguration")]
public sealed class TestUnmappedRootConfigurationNoValidation : UnvalidatedConfiguration<RecursiveCascadeSaveTests.TestUnmappedRootConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TestUnmappedRootConfigurationNoValidation"/> class.</summary>
    public TestUnmappedRootConfigurationNoValidation()
        : base("TestUnmappedRootConfiguration", "A test stand-in; the cascade save test checks how children are written, not what a record may hold.")
    {
    }
}
