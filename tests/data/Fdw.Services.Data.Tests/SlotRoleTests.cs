using System;
using System.IO;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Slots;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A copy of an A/B slot app may do its background work only while its role file says <c>live</c> (FDW-5846). The file is read on every
/// call, a host that is not on a slot is not gated, and a copy that cannot tell which role it has is refused or does nothing, with a code.
/// </summary>
[Collection(nameof(SlotFileEnvironmentCollection))]
public sealed class SlotRoleTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"slotrole-{Guid.NewGuid():N}");

    public SlotRoleTests()
    {
        Directory.CreateDirectory(_directory);
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, null);
        Directory.Delete(_directory, true);
    }

    private string RoleFile(string content)
    {
        var path = Path.Combine(_directory, "role");
        File.WriteAllText(path, content);
        return path;
    }

    private string OnSlot(string slotJson)
    {
        var path = Path.Combine(_directory, "slot-a.json");
        File.WriteAllText(path, slotJson);
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, path);
        return path;
    }

    private ISlotRole GateFor(string roleFileContent)
    {
        var roleFile = RoleFile(roleFileContent);
        OnSlot($$"""{ "role_file": "{{roleFile.Replace("\\", "\\\\", StringComparison.Ordinal)}}" }""");
        var created = SlotRole.Create(NullLogger.Instance);
        created.IsSuccess.ShouldBeTrue();
        var gate = created.Value;
        gate.ShouldNotBeNull();
        return gate;
    }

    private static string Codes(IGenericResult result) => string.Join(" ", result.Messages.Select(message => message.Code));

    private static string Text(IGenericResult result) => string.Join(" | ", result.Messages.Select(message => message.Message));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void AHostThatIsNotOnASlotMayRun()
    {
        var created = SlotRole.Create(NullLogger.Instance);

        created.IsSuccess.ShouldBeTrue();
        var answer = created.Value?.BackgroundWorkMayRun();
        answer.ShouldNotBeNull();
        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void TheLiveCopyMayRun()
    {
        var answer = GateFor("live\n").BackgroundWorkMayRun();

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void TheStandbyCopyMayNotRun()
    {
        var answer = GateFor("standby").BackgroundWorkMayRun();

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void ASwapUnderARunningCopyChangesTheNextAnswer()
    {
        var gate = GateFor("live");
        gate.BackgroundWorkMayRun().Value.ShouldBe(true);

        File.WriteAllText(Path.Combine(_directory, "role"), "standby");

        gate.BackgroundWorkMayRun().Value.ShouldBe(false);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    [InlineData("")]
    [InlineData("Live")]
    [InlineData("active")]
    public void ARoleFileThatSaysAnythingElseIsAFailureNamingTheFileAndWhatItHolds(string content)
    {
        var gate = GateFor(content);

        var answer = gate.BackgroundWorkMayRun();

        answer.IsFailure.ShouldBeTrue();
        Codes(answer).ShouldContain("DATA-61118");
        Text(answer).ShouldContain(Path.Combine(_directory, "role"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void ARoleFileThatIsNotThereIsAFailureNamingIt()
    {
        var gate = GateFor("live");
        File.Delete(Path.Combine(_directory, "role"));

        var answer = gate.BackgroundWorkMayRun();

        answer.IsFailure.ShouldBeTrue();
        Codes(answer).ShouldContain("DATA-61117");
        Text(answer).ShouldContain(Path.Combine(_directory, "role"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void ASlotFileThatStatesNoRoleFileStopsTheHostNamingTheFile()
    {
        var slot = OnSlot("""{ "app": "techcom" }""");

        var created = SlotRole.Create(NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        Codes(created).ShouldContain("DATA-61116");
        Text(created).ShouldContain(slot);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void ASlotFileThatIsNotThereStopsTheHostNamingTheFile()
    {
        var missing = Path.Combine(_directory, "nothing.json");
        Environment.SetEnvironmentVariable(SlotFile.EnvironmentVariableName, missing);

        var created = SlotRole.Create(NullLogger.Instance);

        created.IsFailure.ShouldBeTrue();
        Codes(created).ShouldContain("DATA-61110");
        Text(created).ShouldContain(missing);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void TheSlotNameIsTheSlotFilesOwnValueAsWritten()
    {
        var roleFile = RoleFile("live");
        OnSlot($$"""{ "slot": "a", "role_file": "{{roleFile.Replace("\\", "\\\\", StringComparison.Ordinal)}}" }""");

        var created = SlotRole.Create(NullLogger.Instance);

        created.IsSuccess.ShouldBeTrue();
        created.Value.ShouldNotBeNull().SlotName.ShouldBe("a");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void ASlotFileThatStatesNoSlotGivesNoSlotName()
    {
        var gate = GateFor("live");

        gate.SlotName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public void AHostThatIsNotOnASlotHasNoSlotName()
    {
        var created = SlotRole.Create(NullLogger.Instance);

        created.IsSuccess.ShouldBeTrue();
        created.Value.ShouldNotBeNull().SlotName.ShouldBeNull();
    }
}
