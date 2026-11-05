using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the create-connection rules every host inherits (FDW-3572).</summary>
public sealed class CreateConnectionRequestValidatorTests
{
    private sealed class Sut : CreateConnectionRequestValidator;

    private static CreateConnectionRequest Valid() => new() { Name = "Reporting", ServiceType = "MsSql" };

    [Fact]
    public void AValidRequestPasses() => new Sut().Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void AnEmptyNameIsRefused()
    {
        var request = Valid();
        request.Name = string.Empty;

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void ANameThatDoesNotStartWithALetterIsRefused()
    {
        var request = Valid();
        request.Name = "1Reporting";

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void ANameUpToTheStoredLengthOf200IsAccepted()
    {
        var request = Valid();
        request.Name = "a" + new string('b', 199);

        new Sut().Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnEmptyServiceTypeIsRefused()
    {
        var request = Valid();
        request.ServiceType = string.Empty;

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "ServiceType");
    }

    [Fact]
    public void AServerOver256CharactersIsRefused()
    {
        var request = Valid();
        request.Server = new string('s', 257);

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Server");
    }

    [Fact]
    public void ARequestForAKindWithNoPortIsNotRefusedForItsPort()
    {
        var request = Valid();
        request.ServiceType = "Http";
        request.Port = 0;

        new Sut().Validate(request).Errors.ShouldNotContain(e => e.PropertyName == "Port");
    }
}
