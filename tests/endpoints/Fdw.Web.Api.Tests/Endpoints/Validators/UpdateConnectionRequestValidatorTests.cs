using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the update-connection rules every host inherits (FDW-3572).</summary>
public sealed class UpdateConnectionRequestValidatorTests
{
    private sealed class Sut : UpdateConnectionRequestValidator;

    [Fact]
    public void ARequestThatStatesAServerPasses() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = "Reporting", Server = "db01" }).IsValid.ShouldBeTrue();

    [Fact]
    public void ARequestThatStatesNothingIsRefused() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = "Reporting" }).Errors
            .ShouldContain(e => e.ErrorCode == UpdateConnectionChanges.NothingStatedCode);

    [Fact]
    public void AnEmptyRouteNameIsRefused() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = string.Empty, Server = "db01" }).Errors
            .ShouldContain(e => e.PropertyName == "Name");

    [Fact]
    public void AStoredNameInAnotherFormIsNotRefusedByANameRule() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = "old name 1", Server = "db01" }).IsValid.ShouldBeTrue();

    [Fact]
    public void APortOutOfRangeIsRefused() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = "Reporting", Port = 70000 }).Errors
            .ShouldContain(e => e.ErrorMessage == "Port must be between 1 and 65535");

    [Fact]
    public void AServerOver256CharactersIsRefused() =>
        new Sut().Validate(new UpdateConnectionRequest { Name = "Reporting", Server = new string('s', 257) }).Errors
            .ShouldContain(e => e.ErrorMessage == "Server must not exceed 256 characters");
}
