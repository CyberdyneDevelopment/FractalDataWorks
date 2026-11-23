using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.TestDouble;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-6584: a key in configurationSchema.json that no class binds is a coded failure, not a key the binder drops without a word. The refusal names the file, the key and
/// where it sits in the file's own words, once for each distinct place a key on fields sits (the first field and how many further fields), every gap in one pass. The bound
/// members are the binder's own (a key that differs only by case or is an alias is bound), and a connection's Configuration is judged against the type its Implementation names.
/// </summary>
[Trait("Category", "SchemaLoader")]
public sealed class ConfigurationSchemaLoaderUnboundKeysTests
{
    private const string Code = "DATA-61147";

    private static readonly Microsoft.Extensions.Logging.ILogger Log = NullLogger.Instance;

    // A schema with a store, a path, a container and two or three fields; the extras are inserted where the test says.
    private static string Schema(string storeKeys = "", string pathKeys = "", string containerKeys = "", string fieldKeys = "", int fields = 3, string connectionKind = "MockConnection", string configuration = "", string extraSections = "")
    {
        // Built by replacing named markers in a plain raw string: an interpolated one cannot carry JSON's own closing braces right after a hole (CS9007).
        var fieldJson = string.Join(",", Enumerable.Range(1, fields).Select(i => """{"Name":"Field@I@","DataType":"int"@FIELDKEYS@}""".Replace("@I@", i.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("@FIELDKEYS@", fieldKeys, StringComparison.Ordinal)));
        return """
            {"ConfigurationSchema":{
              "Connections":[{"Name":"Platform","Implementation":"@KIND@","Configuration":{"Root":"x"@CONFIGURATION@}}],
              "DataStores":[{"Name":"PlatformConfiguration","Implementation":"MockConnection"@STOREKEYS@,
                "Paths":[{"Name":"tenant","PathType":"DatabasePath"@PATHKEYS@,
                  "Containers":[{"Name":"Tenants","TypeId":"Table"@CONTAINERKEYS@,"Fields":[@FIELDS@]}]}]}]@EXTRA@}}
            """
            .Replace("@KIND@", connectionKind, StringComparison.Ordinal)
            .Replace("@CONFIGURATION@", configuration, StringComparison.Ordinal)
            .Replace("@STOREKEYS@", storeKeys, StringComparison.Ordinal)
            .Replace("@PATHKEYS@", pathKeys, StringComparison.Ordinal)
            .Replace("@CONTAINERKEYS@", containerKeys, StringComparison.Ordinal)
            .Replace("@FIELDS@", fieldJson, StringComparison.Ordinal)
            .Replace("@EXTRA@", extraSections, StringComparison.Ordinal);
    }

    private static IGenericResult<Fdw.Services.Data.Configuration.ConfigurationSchema> Load(string schemaJson)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6584-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, schemaJson);
        try
        {
            return ConfigurationSchemaLoader.Load(path, Log);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    private static string[] Refusals(string schemaJson)
    {
        var loaded = Load(schemaJson);
        loaded.IsFailure.ShouldBeTrue(string.Concat("expected a refusal, got: ", loaded.CurrentMessage));
        return [.. loaded.Messages.Where(m => m.Code == Code).Select(m => m.Message)];
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACleanSchemaLoads()
        => Load(Schema()).IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnAFieldIsRefusedNamingTheKeyAndTheFieldInTheFilesOwnWords()
    {
        var refusals = Refusals(Schema(fieldKeys: ""","Visible":false""", fields: 1));

        var only = refusals.ShouldHaveSingleItem();
        only.ShouldContain("'Visible'");
        only.ShouldContain("data store 'PlatformConfiguration'");
        only.ShouldContain("path 'tenant'");
        only.ShouldContain("container 'Tenants'");
        only.ShouldContain("field 'Field1'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnManyFieldsOfOneContainerIsOneMessageNamingTheFirstFieldAndTheCountOfTheOthers()
    {
        var refusals = Refusals(Schema(fieldKeys: ""","Visible":true""", fields: 3));

        var only = refusals.ShouldHaveSingleItem();
        only.ShouldContain("field 'Field1'");
        only.ShouldContain("2 further");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnAPathIsRefusedNamingThePath()
    {
        var only = Refusals(Schema(pathKeys: ""","Path":null""")).ShouldHaveSingleItem();

        only.ShouldContain("'Path'");
        only.ShouldContain("data store 'PlatformConfiguration'");
        only.ShouldContain("path 'tenant'");
        only.ShouldNotContain("container");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnAContainerIsRefusedNamingTheContainer()
    {
        var only = Refusals(Schema(containerKeys: ""","NoSuchMember":1""")).ShouldHaveSingleItem();

        only.ShouldContain("'NoSuchMember'");
        only.ShouldContain("container 'Tenants'");
        only.ShouldNotContain("field");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnAStoreIsRefusedNamingTheStore()
    {
        var only = Refusals(Schema(storeKeys: ""","NoSuchMember":1""")).ShouldHaveSingleItem();

        only.ShouldContain("data store 'PlatformConfiguration'");
        only.ShouldNotContain("path");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnAConnectionsConfigurationIsRefusedNamingTheConnection()
    {
        var only = Refusals(Schema(configuration: ""","NoSuchMember":1""")).ShouldHaveSingleItem();

        only.ShouldContain("'NoSuchMember'");
        only.ShouldContain("connection 'Platform'");
        only.ShouldContain("configuration");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyOnTheConnectionRolesSectionIsRefused()
    {
        var only = Refusals(Schema(extraSections: ""","ConnectionRoles":{"Platform":"Platform","NoSuchRole":"x"}""")).ShouldHaveSingleItem();

        only.ShouldContain("'NoSuchRole'");
        only.ShouldContain("connection roles");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AKeyThatDiffersFromABoundMemberOnlyByCaseIsNotRefused()
        => Load(Schema(fieldKeys: ""","isnullable":true""", pathKeys: ""","pathvalue":"tenant" """.Trim())).IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void AnAliasTheClassBindsIsNotRefused()
        // PathType is bound at DataPathConfiguration as an alias of TypeId, so the key the databases view writes there is a bound member (the schema every test here starts from states it).
        => Load(Schema()).IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void AMemberOnlyAnotherConnectionKindBindsIsRefusedOnThisKindAndNotOnItsOwn()
    {
        var refused = Refusals(Schema(connectionKind: "MockConnection", configuration: ""","OnlyThisKindBindsThis":"x" """.Trim()));
        refused.ShouldHaveSingleItem().ShouldContain("'OnlyThisKindBindsThis'");

        Load(Schema(connectionKind: StrictKeysProbeConnectionType.OptionName, configuration: ""","OnlyThisKindBindsThis":"x" """.Trim())).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EveryGapIsReportedInOnePass()
    {
        var refusals = Refusals(Schema(storeKeys: ""","S":1""", pathKeys: ""","P":1""", containerKeys: ""","C":1""", fieldKeys: ""","F":1""", configuration: ""","X":1"""));

        refusals.Length.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnEntryWhoseKindNobodyRegisteredKeepsItsOwnRefusalAndIsNotJudgedByThisPass()
    {
        var loaded = Load(Schema(connectionKind: "NoSuchConnectionKind", configuration: ""","NoSuchMember":1"""));

        loaded.IsFailure.ShouldBeTrue();
        loaded.Messages.Select(m => m.Code).ShouldContain("DATA-61025");
        loaded.Messages.Select(m => m.Code).ShouldNotContain(Code);
    }
}
