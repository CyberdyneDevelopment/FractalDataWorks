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
/// A row may state <c>${KEY}</c> placeholders that the host fills from its slot file or its environment (FDW-5847). A text with no
/// placeholder is a literal and is never touched; a key stated twice or not at all is refused with a code that says the row and the key.
/// </summary>
[Collection(nameof(SlotFileEnvironmentCollection))]
public sealed class SlotPlaceholdersTests : IDisposable
{
    private const string Owner = "OIDC authority 'reference' AllowedRedirectOrigins";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slotplaceholders-{Guid.NewGuid():N}");

    public SlotPlaceholdersTests()
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
        Environment.SetEnvironmentVariable("SLOTROW_HOST", null);
    }

    private string OnSlot(string json)
    {
        var path = Path.Combine(_directory, "slot-a.json");
        File.WriteAllText(path, json);
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, path);
        return path;
    }

    private static string Codes(IGenericResult result) => string.Join(" ", result.Messages.Select(message => message.Code));

    private static string Text(IGenericResult result) => string.Join(" | ", result.Messages.Select(message => message.Message));

    private static string? Resolve(string text) => SlotPlaceholders.Resolve(text, Owner, NullLogger.Instance).Value;

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData("https://reference-ui.example.dev")]
    [InlineData("")]
    [InlineData("a $ {not} a placeholder and ${ also not and $KEY neither")]
    public void ALiteralIsReturnedAsItIs(string literal)
    {
        var result = SlotPlaceholders.Resolve(literal, Owner, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(literal);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ALiteralOnASlotHostDoesNotReadTheSlotFile()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, Path.Combine(_directory, "there-is-no-such-file.json"));

        var result = SlotPlaceholders.Resolve("https://reference-ui.example.dev", Owner, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("https://reference-ui.example.dev");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void APlaceholderTakesTheSlotFilesValueOfItsKeyPath()
    {
        OnSlot("""{ "hostnames": { "live": ["techcom.example.dev"], "standby": ["techcom-internal.example.dev"] } }""");

        Resolve("https://${hostnames__live__0}, https://${hostnames__standby__0}")
            .ShouldBe("https://techcom.example.dev, https://techcom-internal.example.dev");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void APlaceholderTakesTheEnvironmentVariableOnAHostThatIsNotOnASlot()
    {
        Environment.SetEnvironmentVariable("SLOTROW_HOST", "from-environment.example.dev");

        Resolve("https://${SLOTROW_HOST}").ShouldBe("https://from-environment.example.dev");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AValueIsWrittenAsItIsNotEncoded()
    {
        OnSlot("""{ "q": "a\"b\\c&d" }""");

        Resolve("${q}").ShouldBe("a\"b\\c&d");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AKeyStatedByTheEnvironmentAndTheSlotFileIsRefusedNamingTheRowTheKeyAndTheFile()
    {
        var slot = OnSlot("""{ "SLOTROW_HOST": "from-file.example.dev" }""");
        Environment.SetEnvironmentVariable("SLOTROW_HOST", "from-environment.example.dev");

        var result = SlotPlaceholders.Resolve("https://${SLOTROW_HOST}", Owner, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61159");
        Text(result).ShouldContain(Owner);
        Text(result).ShouldContain("SLOTROW_HOST");
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AKeyTheSlotFileDoesNotStateIsRefusedNamingTheRowTheKeyAndTheFile()
    {
        var slot = OnSlot("""{ "hostnames": { "live": ["techcom.example.dev"] } }""");

        var result = SlotPlaceholders.Resolve("https://${hostnames__standby__0}", Owner, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61160");
        Text(result).ShouldContain(Owner);
        Text(result).ShouldContain("hostnames__standby__0");
        Text(result).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AKeyOnAHostThatIsNotOnASlotAndHasNoVariableIsRefusedNamingTheRowAndTheKey()
    {
        var result = SlotPlaceholders.Resolve("https://${hostnames__live__0}", Owner, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61161");
        Text(result).ShouldContain(Owner);
        Text(result).ShouldContain("hostnames__live__0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AnUnreadableSlotFileIsRefusedWithItsOwnCodeAndTheRowAndTheKeys()
    {
        var missing = Path.Combine(_directory, "nothing.json");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, missing);

        var result = SlotPlaceholders.Resolve("https://${hostnames__live__0}", Owner, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldContain("DATA-61110");
        Codes(result).ShouldContain("DATA-61162");
        Text(result).ShouldContain(missing);
        Text(result).ShouldContain(Owner);
        Text(result).ShouldContain("hostnames__live__0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EveryKeyThatIsMissingIsListedOnceInTheOneRefusal()
    {
        OnSlot("""{ "present": "x" }""");

        var result = SlotPlaceholders.Resolve("${absent_one} ${present} ${absent_two} ${absent_one}", Owner, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Text(result).ShouldContain("absent_one, absent_two");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("https://x.example.dev", false)]
    [InlineData("https://${hostnames__live__0}", true)]
    [InlineData("${a}${b}", true)]
    public void HasSaysWhetherATextStatesAPlaceholder(string? text, bool expected)
        => SlotPlaceholders.Has(text).ShouldBe(expected);
}
