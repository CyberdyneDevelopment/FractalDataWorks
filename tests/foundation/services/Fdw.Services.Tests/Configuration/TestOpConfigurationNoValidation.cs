using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>States that the cascade-save test stand-in <see cref="RecursiveCascadeSaveTests.TestOpConfiguration"/> needs no validator (FDW-3541).</summary>
[TypeOption(typeof(ConfigurationValidators), "TestOpConfiguration")]
public sealed class TestOpConfigurationNoValidation : UnvalidatedConfiguration<RecursiveCascadeSaveTests.TestOpConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TestOpConfigurationNoValidation"/> class.</summary>
    public TestOpConfigurationNoValidation()
        : base("TestOpConfiguration", "A test stand-in; the cascade save test checks how children are written, not what a record may hold.")
    {
    }
}
