using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>States that the cascade-save test stand-in <see cref="RecursiveCascadeSaveTests.TestMapConfiguration"/> needs no validator (FDW-3541).</summary>
[TypeOption(typeof(ConfigurationValidators), "TestMapConfiguration")]
public sealed class TestMapConfigurationNoValidation : UnvalidatedConfiguration<RecursiveCascadeSaveTests.TestMapConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TestMapConfigurationNoValidation"/> class.</summary>
    public TestMapConfigurationNoValidation()
        : base("TestMapConfiguration", "A test stand-in; the cascade save test checks how children are written, not what a record may hold.")
    {
    }
}
