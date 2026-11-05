using System.Runtime.CompilerServices;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Registers this assembly's own test options with <see cref="ConfigurationValidators"/>: the registration generator registers options found in
/// referenced assemblies, never a project's own (FDW-3541). <c>RegisterMember</c> is idempotent.
/// </summary>
internal static class TestConfigurationValidatorRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        ConfigurationValidators.RegisterMember(new TestRootConfigurationNoValidation());
        ConfigurationValidators.RegisterMember(new TestUnmappedRootConfigurationNoValidation());
        ConfigurationValidators.RegisterMember(new TestOpConfigurationNoValidation());
        ConfigurationValidators.RegisterMember(new TestMapConfigurationNoValidation());
    }
}
