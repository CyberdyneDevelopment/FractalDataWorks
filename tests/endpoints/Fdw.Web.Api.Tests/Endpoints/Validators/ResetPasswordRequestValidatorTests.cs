using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the reset-password rules every host inherits (FDW-3572).</summary>
public sealed class ResetPasswordRequestValidatorTests
{
    private sealed class Sut : ResetPasswordRequestValidator<ResetPasswordRequest>;

    [Fact]
    public void APasswordOfEightCharactersPasses() =>
        new Sut().Validate(new ResetPasswordRequest { IdOrName = "analyst_one", NewPassword = "12345678" }).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void AMissingOrShortPasswordIsRefused(string password) =>
        new Sut().Validate(new ResetPasswordRequest { IdOrName = "analyst_one", NewPassword = password }).Errors
            .ShouldContain(e => e.PropertyName == "NewPassword");
}
