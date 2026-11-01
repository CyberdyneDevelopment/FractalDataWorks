using System.Text.Json;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// FDW-3171: an update that sends part of a calculation's configuration keeps the members it does not send, refuses a member sent as
/// null, and reads a configuration of another kind whole. Before, a calculation holding a configuration went back to the class defaults
/// for every member a PATCH did not send, and nothing said so.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationTypedConfigurationReaderTests
{
    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static WindowedCalculationConfiguration StoredWindowed() => new()
    {
        TargetField = "WeightLbs",
        WindowFunction = "Sum"
    };

    [Fact]
    public void A_partial_update_keeps_the_members_it_does_not_send()
    {
        var result = CalculationTypedConfigurationReader.ReadOver(
            "Windowed", StoredWindowed(), Json("""{"windowFunction":"Max"}"""), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var windowed = result.Value.ShouldBeOfType<WindowedCalculationConfiguration>();
        windowed.WindowFunction.ShouldBe("Max");
        windowed.TargetField.ShouldBe("WeightLbs");
    }

    [Fact]
    public void A_member_sent_in_another_case_replaces_the_stored_one()
    {
        var result = CalculationTypedConfigurationReader.ReadOver(
            "Windowed", StoredWindowed(), Json("""{"TargetField":"HeightInches"}"""), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var windowed = result.Value.ShouldBeOfType<WindowedCalculationConfiguration>();
        windowed.TargetField.ShouldBe("HeightInches");
        windowed.WindowFunction.ShouldBe("Sum");
    }

    [Fact]
    public void An_update_that_sends_an_undeclared_member_is_refused()
    {
        var result = CalculationTypedConfigurationReader.ReadOver(
            "Windowed", StoredWindowed(), Json("""{"colour":"red"}"""), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21539");
    }

    [Theory]
    [InlineData("""{"windowFunction":null}""")]
    [InlineData("""{"targetField":null}""")]
    public void An_update_that_sends_a_member_as_null_is_refused_and_not_reset(string sent)
    {
        var result = CalculationTypedConfigurationReader.ReadOver("Windowed", StoredWindowed(), Json(sent), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21546");
    }

    [Fact]
    public void A_create_that_sends_a_member_as_null_is_refused()
    {
        var result = CalculationTypedConfigurationReader.Read(
            "Windowed", Json("""{"targetField":"WeightLbs","windowFunction":null}"""), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ENDPOINTS-21546");
    }

    [Fact]
    public void A_stored_configuration_of_another_kind_is_not_merged_and_the_sent_one_is_read_whole()
    {
        var result = CalculationTypedConfigurationReader.ReadOver(
            "Windowed", new UnregisteredCalculationConfiguration(), Json("""{"targetField":"WeightLbs","windowFunction":"Sum"}"""), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var windowed = result.Value.ShouldBeOfType<WindowedCalculationConfiguration>();
        windowed.TargetField.ShouldBe("WeightLbs");
        windowed.WindowFunction.ShouldBe("Sum");
    }

    [Fact]
    public void A_member_of_the_wrong_type_is_refused_by_name_and_without_framework_text()
    {
        var result = CalculationTypedConfigurationReader.Read(
            "Windowed", Json("""{"targetField":"WeightLbs","windowFunction":5}"""), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        var refusal = result.Messages[^1];
        refusal.Code.ShouldBe("ENDPOINTS-21531");
        var clientText = refusal.ShouldBeAssignableTo<IClientMessage>().ClientText;
        clientText.ShouldContain("windowFunction");
        clientText.ShouldContain("text");
        clientText.ShouldNotContain("System.");
        clientText.ShouldNotContain("Path:");
        clientText.ShouldNotContain("LineNumber");
        clientText.ShouldNotContain("BytePosition");
    }

    [Fact]
    public void A_configuration_that_is_not_an_object_is_refused_without_framework_text()
    {
        var result = CalculationTypedConfigurationReader.Read("Windowed", Json("[1,2]"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        var refusal = result.Messages[^1];
        refusal.Code.ShouldBe("ENDPOINTS-21531");
        refusal.ShouldBeAssignableTo<IClientMessage>().ClientText.ShouldContain("configuration must be a JSON object");
    }
}
