using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// A schedule's description lives in the descriptions table and not on <c>sched.ScheduleConfiguration</c> (FDW-6208): the provider describes the schedule in the unit
/// of its save, fills the text once for every set of composed schedules it reads (a read by id, by name and a page are each one such set) and removes it with the
/// schedule. The writer and reader are strict stand-ins; <c>DescriptionServiceTests</c> covers what they do.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ScheduleDescriptionsTests
{
    private static readonly Guid Nightly = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Hourly = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private readonly Mock<IDescriptionWriter> _writer = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionReader> _reader = new(MockBehavior.Strict);

    private ScheduleConfigurationProvider Schedules()
        => new(
            NullLogger<ScheduleConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object, "PlatformConfiguration", "sched", "Schedule",
            _writer.Object, _reader.Object);

    // The reader answers with the texts it holds, put on each schedule through the provider's own Id and fill delegates.
    private void TheReaderHolds(Dictionary<Guid, string> texts)
        => _reader
            .Setup(r => r.Fill(
                "Schedule", "Text", It.IsAny<IReadOnlyList<IScheduleImplementationConfiguration>>(), It.IsAny<Func<IScheduleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IScheduleImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<IScheduleImplementationConfiguration>, Func<IScheduleImplementationConfiguration, Guid>, Action<IScheduleImplementationConfiguration, string>, CancellationToken>(
                (_, _, things, idOf, fill, _) =>
                {
                    foreach (var thing in things)
                    {
                        if (texts.TryGetValue(idOf(thing), out var text)) fill(thing, text);
                    }
                })
            .ReturnsAsync(GenericResult.Success());

    [Fact]
    [Trait("Priority", "P0")]
    public void TheProviderDeclaresTheWriteAndReadObserversAndTheRemovalSoEveryPathOfTheBaseCallsThem()
    {
        var schedules = Schedules();

        schedules.ShouldBeAssignableTo<IConfigurationWriteObserver<IScheduleImplementationConfiguration>>();
        schedules.ShouldBeAssignableTo<IConfigurationReadObserver<IScheduleImplementationConfiguration>>();
        schedules.ShouldBeAssignableTo<IReferencedConfigurationProvider<IScheduleImplementationConfiguration>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRecordsMapperHasNoDescriptionSoNoReadSelectsTheColumnAndNoSaveWritesIt()
    {
        new ScheduleImplementationConfigurationPocoMapper().GetPropertyNames().ShouldNotContain("Description");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("Runs the nightly load of the order data sets.")]
    [InlineData(null)]
    [InlineData("")]
    public async Task ASavedScheduleIsDescribedUnderTheScheduleTypeWithTheTextItCarriesOrNone(string? text)
    {
        _writer
            .Setup(w => w.Describe("Schedule", "Text", Nightly, "nightly", text, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var written = await Schedules().Written(
            new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly", Description = text }, null!, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedDescriptionFailsTheSaveSoItsUnitRollsBack()
    {
        _writer
            .Setup(w => w.Describe("Schedule", "Text", Nightly, "nightly", "Nightly", It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var written = await Schedules().Written(
            new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly", Description = "Nightly" }, null!, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OneFillPutsTheTextOnEveryComposedScheduleAndOneWithNoneStaysEmpty()
    {
        var schedules = new List<IScheduleImplementationConfiguration>
        {
            new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly" },
            new ScheduleImplementationConfiguration { Id = Hourly, Name = "hourly" },
        };
        TheReaderHolds(new Dictionary<Guid, string> { [Nightly] = "Runs the nightly load" });

        var read = await Schedules().Read(schedules, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        schedules[0].Description.ShouldBe("Runs the nightly load");
        schedules[1].Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadOfOneScheduleByIdOrByNameIsAFillOfASetOfOne()
    {
        var byId = new List<IScheduleImplementationConfiguration> { new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly" } };
        var byName = new List<IScheduleImplementationConfiguration> { new ScheduleImplementationConfiguration { Id = Hourly, Name = "hourly" } };
        TheReaderHolds(new Dictionary<Guid, string> { [Nightly] = "Runs the nightly load", [Hourly] = "Runs every hour" });

        (await Schedules().Read(byId, null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await Schedules().Read(byName, null, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        byId[0].Description.ShouldBe("Runs the nightly load");
        byName[0].Description.ShouldBe("Runs every hour");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AFailedFillFailsTheRead()
    {
        _reader
            .Setup(r => r.Fill(
                "Schedule", "Text", It.IsAny<IReadOnlyList<IScheduleImplementationConfiguration>>(), It.IsAny<Func<IScheduleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IScheduleImplementationConfiguration, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("language not stated")));

        var read = await Schedules().Read([new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly" }], null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EachReadAndWritePassesTheCallersToken()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _reader
            .Setup(r => r.Fill(
                "Schedule", "Text", It.IsAny<IReadOnlyList<IScheduleImplementationConfiguration>>(), It.IsAny<Func<IScheduleImplementationConfiguration, Guid>>(),
                It.IsAny<Action<IScheduleImplementationConfiguration, string>>(), cancelled.Token))
            .ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.Describe("Schedule", "Text", Nightly, "nightly", "Nightly", It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());
        _writer.Setup(w => w.DeleteAbout("Schedule", Nightly, It.IsAny<ConfigurationWriteScope>(), cancelled.Token)).ReturnsAsync(GenericResult.Success());

        await Schedules().Read([new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly" }], null, cancelled.Token);
        await Schedules().Written(new ScheduleImplementationConfiguration { Id = Nightly, Name = "nightly", Description = "Nightly" }, null!, cancelled.Token);
        await Schedules().RemoveReferences(Nightly, null!, cancelled.Token);

        _reader.VerifyAll();
        _writer.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task DeletingAScheduleRemovesItsDescriptionsInEveryLanguage()
    {
        _writer.Setup(w => w.DeleteAbout("Schedule", Nightly, It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        (await Schedules().RemoveReferences(Nightly, null!, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _writer.VerifyAll();
    }
}
