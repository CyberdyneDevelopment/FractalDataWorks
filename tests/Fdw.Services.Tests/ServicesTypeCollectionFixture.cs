using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Services.Configuration;
using Fdw.Services.Results;
using Fdw.Services.Tests.Configuration;
using Xunit;

namespace Fdw.Services.Tests;

/// <summary>
/// Fixture that ensures ServicesResultCodes TypeCollection is fully initialized
/// before any tests run. Required because RestrictToCurrentCompilation prevents
/// auto-registration of TypeOptions in the test assembly.
/// </summary>
/// <remarks>
/// Why: TypeOptionModuleInitializerGenerator only scans referenced assemblies, not the
/// current (test) assembly. Types defined in the test assembly itself must be registered
/// manually before any TypeCollection lookup freezes the collection.
/// </remarks>
public sealed class ServicesTypeCollectionFixture
{
    public ServicesTypeCollectionFixture()
    {
        _ = ServicesResultCodes.All();
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestConfigurationCommand());
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderGetByIdTests.TestChildCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestRootCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestBodyCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestOpCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestMapCommand());
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestKvpCommand());
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestContainerCommand());
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestContainerFieldCommand());

        PocoMapperCollection.RegisterMember(new TestRootConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestBodyConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestOpConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestMapConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestKvpConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestContainerConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestContainerFieldConfigurationPocoMapper());
    }
}

[CollectionDefinition(nameof(ServicesTestCollection))]
public sealed class ServicesTestCollection : ICollectionFixture<ServicesTypeCollectionFixture>
{
}
