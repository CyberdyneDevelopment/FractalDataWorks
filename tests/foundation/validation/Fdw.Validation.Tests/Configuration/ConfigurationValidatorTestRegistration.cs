using System.Runtime.CompilerServices;
using Fdw.Validation.Configuration;

namespace Fdw.Validation.Tests.Configuration;

/// <summary>
/// Registers this assembly's test options with <see cref="ConfigurationValidators"/>. The registration generator emits a module initializer only
/// for options found in the assemblies a project references, never for the project's own, so an option declared in a test assembly is not
/// registered unless the test does it (FDW-3541). <c>RegisterMember</c> is idempotent.
/// </summary>
internal static class ConfigurationValidatorTestRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        ConfigurationValidators.RegisterMember(new ResolverBaseOption());
        ConfigurationValidators.RegisterMember(new ResolverOwnOption());
        ConfigurationValidators.RegisterMember(new ResolverContractOption());
        ConfigurationValidators.RegisterMember(new ResolverOtherContractOption());
        ConfigurationValidators.RegisterMember(new CodedRefusalOption());
    }
}
