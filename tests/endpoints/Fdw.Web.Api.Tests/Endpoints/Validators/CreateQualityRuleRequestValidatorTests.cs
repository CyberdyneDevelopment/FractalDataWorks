using Fdw.Services.Quality.Endpoints;
using Fdw.Services.Quality.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the create-quality-rule rules every host inherits (FDW-3572).</summary>
public sealed class CreateQualityRuleRequestValidatorTests
{
    private sealed class Sut : CreateQualityRuleRequestValidator;

    private static CreateQualityRuleRequest Valid() => new() { DataSetName = "AfcPlayers", RuleType = "NotNull", Severity = "Warning" };

    [Fact]
    public void AValidRequestPasses() => new Sut().Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void AnEmptyDataSetNameIsRefused()
    {
        var request = Valid();
        request.DataSetName = string.Empty;

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "DataSetName");
    }

    [Fact]
    public void ASeverityOutsideTheFourIsRefused()
    {
        var request = Valid();
        request.Severity = "Fatal";

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Severity");
    }

    [Fact]
    public void ARequestThatCarriesNoNameIsNotRefusedForTheNameTheConfigurationValidatorRequires() =>
        new Sut().Validate(Valid()).Errors.ShouldNotContain(e => e.PropertyName == "Name");
}
