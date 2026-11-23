using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.TestDouble;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.TestDouble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The secret managers a host declares in its schema, and the start-up refusals when they cannot give a connection its secret (FDW-6360). Each refusal is its own coded
/// message with its own test, and none of them names a secret value. The kinds are test doubles: Synthetic reads <c>prefix + key</c> from the environment, SyntheticNeedsStore
/// states it cannot be built from a declaration alone, and SecretProbe is a connection kind with one secret credential.
/// </summary>
[Trait("Category", "SchemaSecretManagers")]
public sealed class SchemaSecretManagersTests
{
    private static readonly ILogger Log = NullLogger.Instance;

    private static ServiceProvider Factories()
        => new ServiceCollection().AddSingleton<ISyntheticSecretManagerFactory>(new SyntheticSecretManagerFactory(NullLoggerFactory.Instance)).BuildServiceProvider();

    private static SyntheticSecretManagerConfiguration Manager(string name, string prefix, string implementation = SyntheticSecretManagerType.OptionName)
        => new() { Name = name, Implementation = implementation, Prefix = prefix };

    private static MockConnectionConfiguration Connection(string name, string manager, string key)
        => new() { Name = name, Implementation = SecretProbeConnectionType.OptionName, Description = SecretProbeConnectionType.Reference(manager, key) };

    private static ConfigurationSchema Schema(IEnumerable<ISecretManagerImplementationConfiguration> managers, params IConnectionImplementationConfiguration[] connections)
    {
        var schema = new ConfigurationSchema();
        foreach (var manager in managers) schema.SecretManagers.Add(manager);
        foreach (var connection in connections) schema.Connections.Add(connection);
        return schema;
    }

    private static string CodeOf(IGenericMessage message) => message.Code.ShouldNotBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AHostThatDeclaresNoManagerGetsAnEmptyProviderThatRefusesEveryNameWithACodedMessage()
    {
        var built = SchemaSecretManagers.Build(Schema([]), Factories(), Log);

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var asked = await built.Value!.Get("anything", TestContext.Current.CancellationToken);
        asked.IsFailure.ShouldBeTrue();
        asked.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SecretManagerNotDeclared(Log, "anything")));
        (await built.Value.Get(TestContext.Current.CancellationToken)).Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADeclaredManagerIsBuiltFromItsDeclarationAndFoundByItsNameWhateverTheCase()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "FDW6360_A_")]), Factories(), Log);

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var found = await built.Value!.Get("bootstrap", TestContext.Current.CancellationToken);
        found.IsSuccess.ShouldBeTrue(found.CurrentMessage);
        found.Value!.Name.ShouldBe("Bootstrap");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AManagerWhoseKindIsNotRegisteredIsRefusedNamingTheManagerAndTheKind()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "X_", "NoSuchKind")]), Factories(), Log);

        built.IsFailure.ShouldBeTrue();
        built.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SecretManagerKindNotRegistered(Log, "Bootstrap", "NoSuchKind")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AManagerWhoseKindCannotBeBuiltWithoutTheConfigurationStoreIsRefusedNamingIt()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "X_", SyntheticNeedsStoreSecretManagerType.OptionName)]), Factories(), Log);

        built.IsFailure.ShouldBeTrue();
        built.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SecretManagerNeedsConfigurationStore(Log, "Bootstrap", SyntheticNeedsStoreSecretManagerType.OptionName)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATwoManagersOfOneNameAreRefusedBecauseAConnectionCouldNotKnowWhichIsMeant()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "A_"), Manager("bootstrap", "B_")]), Factories(), Log);

        built.IsFailure.ShouldBeTrue();
        built.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SecretManagerDeclaredTwice(Log, "bootstrap")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AManagerWithNoKindIsRefusedNamingIt()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "A_", implementation: string.Empty)]), Factories(), Log);

        built.IsFailure.ShouldBeTrue();
        built.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SecretManagerDeclaresNoKind(Log, "Bootstrap")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheProviderOfTheDeclaredManagersIsReadOnlyAndRefusesToRegisterAnyOtherWithACodedMessage()
    {
        var built = SchemaSecretManagers.Build(Schema([Manager("Bootstrap", "A_")]), Factories(), Log).Value!;

        var registered = built.Register("Synthetic", () => throw new InvalidOperationException("never called"));
        var byId = await built.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        registered.IsFailure.ShouldBeTrue();
        registered.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.SchemaSecretManagersAreReadOnly(Log, "Register")));
        byId.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionThatNamesAManagerTheSchemaDoesNotDeclareIsRefusedNamingBoth()
    {
        var schema = Schema([Manager("Bootstrap", "A_")], Connection("Platform", "ghost", "KEY"));
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsFailure.ShouldBeTrue();
        checkedSchema.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.ConnectionNamesUndeclaredSecretManager(Log, "Platform", "ghost")));
    }

    // FDW-6387, FDW-6331: a connection that names no authentication method reads no secret, so registering it is not refused for want of credential
    // slots (a schema valid on 1.5 builds on 1.6); the same connection is still refused by CredentialSlots where connections are saved and read.
    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionThatNamesNoAuthenticationMethodIsNotAskedForCredentialSlotsAndRegistersClean()
    {
        var noMethod = new MockConnectionConfiguration { Name = "synthetic-echo", Implementation = SecretProbeConnectionType.OptionName };
        var schema = Schema([Manager("Bootstrap", "FDW6387_A_")], noMethod);
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsSuccess.ShouldBeTrue(checkedSchema.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACredentialSlotsAskOfAConnectionThatNamesNoMethodStillAnswersCredentialsUndetermined()
    {
        var noMethod = new MockConnectionConfiguration { Name = "synthetic-echo", Implementation = SecretProbeConnectionType.OptionName };
        var kind = Fdw.Services.Connections.ConnectionTypes.ByName(SecretProbeConnectionType.OptionName);

        var slots = kind.CredentialSlots(noMethod);

        slots.IsFailure.ShouldBeTrue();
        slots.CurrentMessage.ShouldNotBeNull().ShouldContain("names no authentication method");
        kind.ReadsStoredSecret(noMethod).Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionThatNeedsASecretAndNamesNoKeyIsRefusedAndNoLoginWithoutAPasswordIsBuilt()
    {
        var schema = Schema([Manager("Bootstrap", "A_")], Connection("Platform", "Bootstrap", string.Empty));
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsFailure.ShouldBeTrue();
        checkedSchema.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.ConnectionNamesNoSecretKey(Log, "Platform", "SecretKeyName")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionThatNamesAKeyAndNoManagerIsRefused()
    {
        var schema = Schema([Manager("Bootstrap", "A_")], Connection("Platform", string.Empty, "KEY"));
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsFailure.ShouldBeTrue();
        checkedSchema.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.ConnectionNamesNoSecretManager(Log, "Platform", "SecretKeyName")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ADeclaredSecretWhoseVariableIsNotSetIsRefusedNamingTheConnectionTheManagerAndTheKey()
    {
        var schema = Schema([Manager("Bootstrap", "FDW6360_UNSET_")], Connection("Platform", "Bootstrap", "NOT_SET_KEY"));
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsFailure.ShouldBeTrue();
        checkedSchema.CurrentMessage.ShouldNotBeNull().ShouldContain("Platform");
        checkedSchema.CurrentMessage.ShouldContain("NOT_SET_KEY");
        checkedSchema.CurrentMessage.ShouldContain("Bootstrap");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ADeclaredSecretWhoseVariableIsSetIsReadAndTheConnectionIsAccepted()
    {
        const string variable = "FDW6360_SET_KEY_ONE";
        Environment.SetEnvironmentVariable(variable, "a-value-that-is-never-printed");
        try
        {
            var schema = Schema([Manager("Bootstrap", "FDW6360_")], Connection("Platform", "Bootstrap", "SET_KEY_ONE"));
            var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

            SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken).IsSuccess.ShouldBeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASecretWhoseValueIsEmptyIsRefusedLikeOneThatIsNotSetAndNeverBuiltIntoAnEmptyPassword()
    {
        var manager = new Moq.Mock<ISecretManager>(Moq.MockBehavior.Strict);
        manager
            .Setup(m => m.Execute(Moq.It.IsAny<ISecretManagerCommand<SecretValue>>(), Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<SecretValue>.Success(new SecretValue("KEY", string.Empty)));
        var provider = new SchemaSecretManagerProvider(new Dictionary<string, ISecretManager> { ["Bootstrap"] = manager.Object });
        var schema = Schema([], Connection("Platform", "Bootstrap", "KEY"));

        var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken);

        checkedSchema.IsFailure.ShouldBeTrue();
        checkedSchema.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(CodeOf(ConfigurationGatewayProviderLog.ConnectionSecretNotAvailable(Log, "Platform", "Bootstrap", "KEY", "the secret's value is empty")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASecretValueNeverReachesALogLineOrAMessageEvenWhenALaterConnectionFailsAfterItWasRead()
    {
        const string variable = "FDW6360_LEAK_KEY";
        const string secret = "leak-canary-5e1c0d7a-FDW6360";
        Environment.SetEnvironmentVariable(variable, secret);
        try
        {
            var captured = new CapturingLogger();
            var schema = Schema(
                [Manager("Bootstrap", "FDW6360_")],
                Connection("First", "Bootstrap", "LEAK_KEY"),
                Connection("Second", "ghost", "LEAK_KEY"));
            var provider = SchemaSecretManagers.Build(schema, Factories(), captured).Value!;

            var checkedSchema = SchemaSecretManagers.CheckConnections(schema, provider, captured, TestContext.Current.CancellationToken);

            checkedSchema.IsFailure.ShouldBeTrue();
            checkedSchema.CurrentMessage.ShouldNotBeNull().ShouldNotContain(secret);
            captured.Lines.ShouldNotBeEmpty();
            captured.Lines.ShouldAllBe(line => !line.Contains(secret, StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKindNobodyRegisteredIsSkippedHereBecauseTheGatewayThatBuildsItRefusesItWithItsOwnMessage()
    {
        var schema = Schema([], new MockConnectionConfiguration { Name = "Platform", Implementation = "NoSuchConnectionKind" });

        SchemaSecretManagers.CheckConnections(schema, SchemaSecretManagers.Build(schema, Factories(), Log).Value!, Log, TestContext.Current.CancellationToken).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASchemaThatDeclaresNoManagerAndConnectionsThatNeedNoSecretIsAcceptedAsItWasBefore()
    {
        // MockConnection has no credential slot at all: nothing is read, nothing is refused, and nothing about the connection changes.
        var schema = Schema([], new MockConnectionConfiguration { Name = "Platform", Implementation = MockConnectionType.OptionName });
        var provider = SchemaSecretManagers.Build(schema, Factories(), Log).Value!;

        SchemaSecretManagers.CheckConnections(schema, provider, Log, TestContext.Current.CancellationToken).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionKindAnswersACodedRefusalForAFactoryThatIsNotItsOwnAndForTheEmptyKind()
    {
        var factory = new Moq.Mock<IConnectionFactory>(Moq.MockBehavior.Strict).Object;
        var provider = SchemaSecretManagers.Build(Schema([]), Factories(), Log).Value!;

        var foreign = Fdw.Services.Connections.ConnectionTypes.ByName(MockConnectionType.OptionName).ImplementationProvider(factory, provider, null);
        var empty = Fdw.Services.Connections.ConnectionTypes.NotFound.ImplementationProvider(factory, provider, null);

        foreign.IsFailure.ShouldBeTrue();
        foreign.Code.ShouldNotBeNull().Name.ShouldBe("ConnectionFactoryOfAnotherKind");
        empty.IsFailure.ShouldBeTrue();
        empty.Code.ShouldNotBeNull().Name.ShouldBe("ConnectionKindNotRegistered");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheLoaderBindsASecretManagerEntryToTheBodyOfTheKindItNames()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6360-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, """{"ConfigurationSchema":{"SecretManagers":[{"Name":"Bootstrap","Implementation":"Synthetic","Configuration":{"Prefix":"FDW6360_"}}]}}""");
        try
        {
            var loaded = ConfigurationSchemaLoader.Load(path, Log);

            loaded.IsSuccess.ShouldBeTrue(loaded.CurrentMessage);
            var manager = loaded.Value!.SecretManagers.ShouldHaveSingleItem().ShouldBeOfType<SyntheticSecretManagerConfiguration>();
            (manager.Name, manager.Implementation, manager.Prefix).ShouldBe(("Bootstrap", "Synthetic", "FDW6360_"));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnEntryThatNamesAKindNobodyRegisteredStopsTheLoadWithItsOwnCodedMessageNamingTheEntry()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6360-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, """{"ConfigurationSchema":{"SecretManagers":[{"Name":"Bootstrap","Implementation":"NoSuchKind","Configuration":{}}]}}""");
        try
        {
            var loaded = ConfigurationSchemaLoader.Load(path, Log);

            loaded.IsFailure.ShouldBeTrue();
            var refusal = loaded.FirstFailureMessage().ShouldNotBeNull().Message;
            refusal.Code.ShouldBe("DATA-61030");
            refusal.Message.ShouldContain("Bootstrap");
            refusal.Message.ShouldContain("NoSuchKind");
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    // FDW-6433, criterion 3: "the code is the message number" is stated for the two connection-secret refusals, so it is read as a number, from a
    // configurationSchema.json on disk through the loader (the way a host reaches it), and from the message's own Code, never result.Code (FDW-6137).
    private static IGenericMessage RefusalOfSchemaFile(string schemaJson)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6433-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, schemaJson);
        try
        {
            var loaded = ConfigurationSchemaLoader.Load(path, Log);
            loaded.IsSuccess.ShouldBeTrue(loaded.CurrentMessage);
            var provider = SchemaSecretManagers.Build(loaded.Value!, Factories(), Log).Value!;

            var checkedSchema = SchemaSecretManagers.CheckConnections(loaded.Value!, provider, Log, TestContext.Current.CancellationToken);

            checkedSchema.IsFailure.ShouldBeTrue();
            return checkedSchema.FirstFailureMessage().ShouldNotBeNull().Message;
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionInTheFileThatNeedsASecretAndNamesNoKeyIsRefusedWithMessageCodeDATA61139()
    {
        var refusal = RefusalOfSchemaFile(
            """{"ConfigurationSchema":{"SecretManagers":[{"Name":"Bootstrap","Implementation":"Synthetic","Configuration":{"Prefix":"FDW6433_"}}],"Connections":[{"Name":"Platform","Implementation":"SecretProbe","Configuration":{"Description":"Bootstrap|"}}]}}""");

        refusal.Code.ShouldBe("DATA-61139");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ADeclaredSecretInTheFileWhoseVariableIsNotSetIsRefusedWithMessageCodeDATA61135()
    {
        var refusal = RefusalOfSchemaFile(
            """{"ConfigurationSchema":{"SecretManagers":[{"Name":"Bootstrap","Implementation":"Synthetic","Configuration":{"Prefix":"FDW6433_UNSET_"}}],"Connections":[{"Name":"Platform","Implementation":"SecretProbe","Configuration":{"Description":"Bootstrap|NOT_SET_KEY"}}]}}""");

        refusal.Code.ShouldBe("DATA-61135");
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Lines.Add(formatter(state, exception) + (exception?.ToString() ?? string.Empty));
    }
}
