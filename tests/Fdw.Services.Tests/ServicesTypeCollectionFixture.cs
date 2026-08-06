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
        // Why: TestConfigurationCommand is defined in this test assembly and is not picked up
        // by TypeOptionModuleInitializerGenerator (which only scans referenced assemblies).
        // Registering here ensures ConfigurationCommands.All().OfType<TestConfigurationCommand>()
        // returns the instance before DefaultConfigurationProvider<TestDualConfig, TestConfigurationCommand>
        // freezes the collection via its static Lazy<TCommand> field.
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestConfigurationCommand());
        // Why: TestChildCommand is also defined in this test assembly and requires the same
        // manual registration. Used by DefaultConfigurationProviderGetByIdTests.
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderGetByIdTests.TestChildCommand());
        // Why: the recursive-cascade test defines a 3-level config hierarchy in this test assembly;
        // each level's command must be registered so SaveOneChild can resolve it via ConfigurationCommands.All().
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestRootCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestBodyCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestOpCommand());
        ConfigurationCommands.RegisterMember(new RecursiveCascadeSaveTests.TestMapCommand());
        // Why: FDW-547 KVP write-cascade regression test defines its own owner command in this
        // test assembly — same manual-registration requirement as the commands above.
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestKvpCommand());
        // Why: FDW-548 typed-list child-cascade regression test (DataContainerConfiguration.Fields
        // equivalent) defines its own owner + child commands in this test assembly — same
        // manual-registration requirement as the commands above.
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestContainerCommand());
        ConfigurationCommands.RegisterMember(new DefaultConfigurationProviderTests.TestContainerFieldCommand());

        // Why: the reflection-free SAVE cascade resolves each level's typed body + child collections
        // through generated PocoMappers. These mappers are generated from the cascade test's POCOs in
        // THIS test assembly, so — like the commands above — the module initializer does not auto-register
        // them; register manually so PocoMapperCollection.ByName(type.Name) resolves during the cascade.
        PocoMapperCollection.RegisterMember(new TestRootConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestBodyConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestOpConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestMapConfigurationPocoMapper());
        // Why: TestKvpConfiguration's generated ReadDictionary (FDW-547) must run for real — its
        // mapper needs the same manual registration as the cascade POCOs above.
        PocoMapperCollection.RegisterMember(new TestKvpConfigurationPocoMapper());
        // Why: TestContainerConfiguration/TestContainerFieldConfiguration's generated typed-list
        // CascadeChildren descriptor (FDW-548) must run for real — same manual registration.
        PocoMapperCollection.RegisterMember(new TestContainerConfigurationPocoMapper());
        PocoMapperCollection.RegisterMember(new TestContainerFieldConfigurationPocoMapper());
    }
}

[CollectionDefinition(nameof(ServicesTestCollection))]
public sealed class ServicesTestCollection : ICollectionFixture<ServicesTypeCollectionFixture>
{
}
