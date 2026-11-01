using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// Tests for <see cref="ScheduleParameterMapper"/> (FDW-1110, contract v2 item 9): the name/value list a schedule body
/// carries and the dictionary the schedule stores are the same values.
/// </summary>
public sealed class ScheduleParameterMapperTests
{
    private static PipelineParameterValue Value(string name, string value) => new() { Name = name, Value = value };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValuesStoredAndReadBackAreTheSameValues()
    {
        var stored = ScheduleParameterMapper.ToConfiguration([Value("Region", "EU"), Value("Since", "2026-10-01T00:00:00Z")]);

        var read = ScheduleParameterMapper.ToValues(stored);

        read.Select(v => (v.Name, v.Value)).ShouldBe([("Region", "EU"), ("Since", "2026-10-01T00:00:00Z")]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AScheduleThatStatesNoValuesStoresNone()
    {
        ScheduleParameterMapper.ToConfiguration(null).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NamesAreDistinctIgnoringCaseAndNeverBlank()
    {
        ScheduleParameterMapper.NamesAreDistinct([Value("Region", "EU"), Value("Since", "x")]).ShouldBeTrue();
        ScheduleParameterMapper.NamesAreDistinct([Value("Region", "EU"), Value("REGION", "US")]).ShouldBeFalse();
        ScheduleParameterMapper.NamesAreDistinct([Value(" ", "EU")]).ShouldBeFalse();
        ScheduleParameterMapper.NamesAreDistinct(null).ShouldBeTrue();
    }
}
