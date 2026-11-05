using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>Tests for the threshold collections (FDW-3936): a name that is not an option answers the NotFound sentinel, and the options the base ships are all there.</summary>
public sealed class ThresholdCollectionsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ANameThatIsNotAnOptionAnswersTheNotFoundSentinelOfEachCollection()
    {
        ReferenceEquals(ThresholdTypes.ByName("NoSuchType"), ThresholdTypes.NotFound).ShouldBeTrue();
        ReferenceEquals(EscalationTypes.ByName("NoSuchEscalation"), EscalationTypes.NotFound).ShouldBeTrue();
        ReferenceEquals(ThresholdSubjectKinds.ByName("NoSuchKind"), ThresholdSubjectKinds.NotFound).ShouldBeTrue();
        ThresholdTypes.NotFound.Name.ShouldNotBeNull();
        EscalationTypes.NotFound.Name.ShouldNotBeNull();
        ThresholdSubjectKinds.NotFound.Name.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheSubjectKindsAreTheSixWithALinkTableEach()
    {
        ThresholdSubjectKinds.All().Select(kind => kind.Name).OrderBy(name => name, StringComparer.Ordinal).ToList()
            .ShouldBe(["Component", "Connection", "DataSet", "Pipeline", "Schedule", "Server"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EverySubjectKindNamesItsOwnLinkTableAndEveryThresholdTypeNamesAKind()
    {
        var kinds = ThresholdSubjectKinds.All().ToList();

        kinds.ShouldAllBe(kind => !string.IsNullOrWhiteSpace(kind.PathName) && kind.ContainerName.EndsWith("Threshold", StringComparison.Ordinal));
        kinds.Select(kind => kind.PathName + "." + kind.ContainerName).Distinct(StringComparer.Ordinal).Count().ShouldBe(kinds.Count);
        ThresholdTypes.All().ShouldAllBe(type => kinds.Any(kind => string.Equals(kind.Name, type.SubjectKind, StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheWaitingRunsTypesReportTheCountOfTheirSubject()
    {
        ThresholdTypes.ByName("PipelineWaits").SubjectKind.ShouldBe("Pipeline");
        ThresholdTypes.ByName("ScheduleWaits").SubjectKind.ShouldBe("Schedule");
        ThresholdTypes.ByName("ServerWaits").SubjectKind.ShouldBe("Server");
        ThresholdTypes.ByName("ServerWaits").Fields.ShouldHaveSingleItem().Name.ShouldBe("waitingRuns");
    }
}
