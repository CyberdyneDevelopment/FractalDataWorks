using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Authorization.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the create-role rules every host inherits (FDW-3572).</summary>
public sealed class CreateRoleRequestValidatorTests
{
    private sealed class Sut : CreateRoleRequestValidator;

    [Fact]
    public void AValidRoleNamePasses() =>
        new Sut().Validate(new CreateRoleRequest { Name = "data-engineer", IsTenantScoped = false }).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("1abc")]
    [InlineData("has space")]
    public void ABadNameIsRefused(string name) =>
        new Sut().Validate(new CreateRoleRequest { Name = name }).Errors.ShouldContain(e => e.PropertyName == "Name");

    [Fact]
    public void ANameThatIsTooShortAnswersWithTheConfigurationValidatorsMessage() =>
        new Sut().Validate(new CreateRoleRequest { Name = "ab" }).Errors
            .ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Role name must be at least 3 characters");

    [Fact]
    public void ANameOver100CharactersIsRefused() =>
        new Sut().Validate(new CreateRoleRequest { Name = "a" + new string('b', 100) }).Errors
            .ShouldContain(e => e.PropertyName == "Name");

    [Fact]
    public void ADisplayNameOver200CharactersIsRefused() =>
        new Sut().Validate(new CreateRoleRequest { Name = "analyst", DisplayName = new string('x', 201) }).Errors
            .ShouldContain(e => e.PropertyName == "DisplayName");

    [Fact]
    public void ADescriptionWithAControlCharacterIsRefusedHereAsTheSaveCheckWouldRefuseIt() =>
        new Sut().Validate(new CreateRoleRequest { Name = "analyst", Description = "bad\u0001value" }).Errors
            .ShouldContain(e => e.PropertyName == "Description" && e.ErrorMessage == "Value contains invalid control characters");

    [Fact]
    public void ADescriptionOfTheColumnsFiveHundredCharactersIsAccepted() =>
        new Sut().Validate(new CreateRoleRequest { Name = "analyst", Description = new string('x', 500), IsTenantScoped = false }).IsValid.ShouldBeTrue();

    [Fact]
    public void ADescriptionOverTheColumnsFiveHundredCharactersIsRefused() =>
        new Sut().Validate(new CreateRoleRequest { Name = "analyst", Description = new string('x', 501) }).Errors
            .ShouldContain(e => e.PropertyName == "Description");
}
