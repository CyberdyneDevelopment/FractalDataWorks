using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>States that the cascade-save test stand-in <see cref="RecursiveCascadeSaveTests.TestRootConfiguration"/> needs no validator (FDW-3541).</summary>
[TypeOption(typeof(ConfigurationValidators), "TestRootConfiguration")]
public sealed class TestRootConfigurationNoValidation : UnvalidatedConfiguration<RecursiveCascadeSaveTests.TestRootConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TestRootConfigurationNoValidation"/> class.</summary>
    public TestRootConfigurationNoValidation()
        : base("TestRootConfiguration", "A test stand-in; the cascade save test checks how children are written, not what a record may hold.")
    {
    }
}
