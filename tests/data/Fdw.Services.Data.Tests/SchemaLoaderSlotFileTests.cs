using System;
using System.IO;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A <c>${NAME}</c> in configurationSchema.json takes its value from the environment or, on a host that runs on an A/B slot
/// (<c>APP_SLOT_FILE</c> set), from the slot file's value of that name (FDW-5845). A name both state, or neither, is a coded
/// failure that names it; with <c>APP_SLOT_FILE</c> unset nothing changes.
/// </summary>
[Collection(nameof(SlotFileEnvironmentCollection))]
public sealed class SchemaLoaderSlotFileTests : IDisposable
{
    private const string SchemaText = """
        { "ConfigurationSchema": { "Connections": [], "ConnectionRoles": { "Platform": "${SLOTTEST_PLATFORM}", "Server": "S" } } }
        """;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slotfile-{Guid.NewGuid():N}");

    public SchemaLoaderSlotFileTests()
    {
        Directory.CreateDirectory(_directory);
        Reset();
    }

    public void Dispose()
    {
        Reset();
        Directory.Delete(_directory, true);
    }

    private static void Reset()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, null);
        Environment.SetEnvironmentVariable("SLOTTEST_PLATFORM", null);
    }

    private string Write(string name, string text)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);
        return path;
    }

    private string Schema() => Write("configurationSchema.json", SchemaText);

    private string SlotFileWith(string json) => Write("slot-a.json", json);

    private static string? Platform(IGenericResult<ConfigurationSchema> result) => result.Value?.ConnectionRoles?.Platform;

    private static string? Server(IGenericResult<ConfigurationSchema> result) => result.Value?.ConnectionRoles?.Server;

    private static string Codes(IGenericResult result) => string.Join(" ", result.Messages.Select(message => message.Code));

    private static string Text(IGenericResult result) => string.Join(" | ", result.Messages.Select(message => message.Message));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void WithNoSlotFileAPlaceholderTakesTheEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("SLOTTEST_PLATFORM", "FromEnvironment");

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe("FromEnvironment");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void WithNoSlotFileAnUnsetVariableIsRefusedNamingIt()
    {
        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61115");
        Text(result).ShouldContain("SLOTTEST_PLATFORM");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void OnASlotAPlaceholderTakesTheSlotFilesValueOfThatName()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "databases": { "x": "y" }, "SLOTTEST_PLATFORM": "FromFile" }"""));

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe("FromFile");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ANestedKeyIsAddressedByItsPathJoinedWithTwoUnderscores()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "SLOTTEST": { "PLATFORM": "Nested" } }"""));
        var path = Write("nested.json", SchemaText.Replace("SLOTTEST_PLATFORM", "SLOTTEST__PLATFORM", StringComparison.Ordinal));

        var result = ConfigurationSchemaLoader.Load(path, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe("Nested");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AValueTheFileAndTheEnvironmentBothStateIsRefusedNamingBoth()
    {
        var slot = SlotFileWith("""{ "SLOTTEST_PLATFORM": "FromFile" }""");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, slot);
        Environment.SetEnvironmentVariable("SLOTTEST_PLATFORM", "FromEnvironment");

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61113");
        Text(result).ShouldContain("SLOTTEST_PLATFORM");
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void OnASlotAValueNeitherSourceStatesIsRefusedNamingTheNameAndTheFile()
    {
        var slot = SlotFileWith("""{ "other": "value" }""");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, slot);

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61114");
        Text(result).ShouldContain("SLOTTEST_PLATFORM");
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AnEmptyStringInTheFileIsNotStated()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "SLOTTEST_PLATFORM": "", "other": null }"""));

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61114");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ASlotFileThatDoesNotExistStopsTheHostNamingIt()
    {
        var missing = Path.Combine(_directory, "nothing-here.json");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, missing);

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61110");
        Text(result).ShouldContain(missing);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ASlotFileThatIsNotJsonStopsTheHostNamingIt()
    {
        var slot = SlotFileWith("not json at all");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, slot);

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61111");
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ASlotFileThatIsNotAnObjectStopsTheHost()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""["a", "b"]"""));

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61111");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TwoEntriesThatFlattenToOneNameStopTheHost()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "a": { "b": "x" }, "a__b": "y" }"""));

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61112");
        Text(result).ShouldContain("a__b");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AValueWithQuotesAndBackslashesReachesTheSchemaIntact()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "SLOTTEST_PLATFORM": "a\"b\\c" }"""));

        var result = ConfigurationSchemaLoader.Load(Schema(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe("a\"b\\c");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NumbersBooleansAndArrayItemsAreStatedByTheirPaths()
    {
        var slot = SlotFile.Read(
            SlotFileWith("""{ "ports": { "web": 5060, "tds": 1433 }, "ok": true, "hostnames": { "live": ["a.example", "b.example"] } }"""),
            NullLogger.Instance);

        slot.IsSuccess.ShouldBeTrue();
        var values = slot.Value?.Values;
        values.ShouldNotBeNull();
        values["ports__tds"].ShouldBe("1433");
        values["ok"].ShouldBe("true");
        values["hostnames__live__1"].ShouldBe("b.example");
    }

    private const string TechComSchemaText = """
        { "ConfigurationSchema": { "Connections": [], "ConnectionRoles": { "Platform": "${databases__TechComConfigurationDb}", "Server": "${databases__TechComAuthDb}" } } }
        """;

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData("""{ "slot": "a", "databases": { "TechComConfigurationDb": "TechComConfigurationDb_A", "TechComAuthDb": "TechComAuthDb_A" } }""", "TechComConfigurationDb_A", "TechComAuthDb_A")]
    [InlineData("""{ "slot": "b", "databases": { "TechComConfigurationDb": "TechComConfigurationDb", "TechComAuthDb": "TechComAuthDb" } }""", "TechComConfigurationDb", "TechComAuthDb")]
    public void EachSlotsCopyOpensItsOwnDatabasesFromItsOwnFile(string file, string configuration, string auth)
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith(file));

        var result = ConfigurationSchemaLoader.Load(Write("techcom.json", TechComSchemaText), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe(configuration);
        Server(result).ShouldBe(auth);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData("""{ "app": "techcom" }""", "databases__TechComConfigurationDb")]
    [InlineData("""{ "databases": [ "TechComDb" ] }""", "databases__TechComConfigurationDb")]
    [InlineData("""{ "databases": { "TechComAuthDb": "A" } }""", "databases__TechComConfigurationDb")]
    [InlineData("""{ "databases": { "TechComConfigurationDb": "C" } }""", "databases__TechComAuthDb")]
    [InlineData("""{ "databases": { "TechComConfigurationDb": "C", "TechComAuthDb": "" } }""", "databases__TechComAuthDb")]
    public void ADatabaseTheFileDoesNotStateIsRefusedNamingTheFileAndTheKey(string file, string key)
    {
        var slot = SlotFileWith(file);
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, slot);

        var result = ConfigurationSchemaLoader.Load(Write("techcom.json", TechComSchemaText), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61114");
        Text(result).ShouldContain(key);
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AHostWithNoSlotPlaceholderIsUntouchedByASlotFile()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, SlotFileWith("""{ "databases": { "x": "y" } }"""));
        var path = Write("plain.json", """{ "ConfigurationSchema": { "Connections": [], "ConnectionRoles": { "Platform": "P", "Server": "S" } } }""");

        var result = ConfigurationSchemaLoader.Load(path, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        Platform(result).ShouldBe("P");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheLoadThatThrowsStillThrowsWithTheFailuresMessage()
    {
        var thrown = Should.Throw<InvalidOperationException>(() => ConfigurationSchemaLoader.Load(Schema()));

        thrown.Message.ShouldContain("SLOTTEST_PLATFORM");
    }
}
