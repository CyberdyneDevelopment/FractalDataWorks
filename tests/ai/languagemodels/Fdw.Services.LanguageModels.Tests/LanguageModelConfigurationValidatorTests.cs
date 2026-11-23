using Fdw.Services.LanguageModels.Validators;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Guard rail 1: the model id is a required setting on the row, and validation refuses a row without it. Nothing here has a default.</summary>
public sealed class LanguageModelConfigurationValidatorTests
{
    private static readonly LanguageModelConfigurationValidator Validator = new();

    private static StubLanguageModelConfiguration Good()
        => StubLanguageModelConfiguration.Carrying(["Chat", "Vision"], ["Low", "High"]);

    private static List<string> Refused(StubLanguageModelConfiguration configuration)
        => Validator.Validate(configuration).Errors.Select(e => e.PropertyName).ToList();

    [Fact]
    public void AFullRowIsAccepted()
        => Validator.Validate(Good()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ARowWithoutAModelIdIsRefused(string? modelId)
    {
        var configuration = Good();
        configuration.ModelId = modelId;

        Refused(configuration).ShouldContain("ModelId");
    }

    [Fact]
    public void ARowWithoutASecretManagerOrASecretNameIsRefused()
    {
        var configuration = Good();
        configuration.SecretManagerName = null;
        configuration.ApiKeySecretName = string.Empty;

        var refused = Refused(configuration);

        refused.ShouldContain("SecretManagerName");
        refused.ShouldContain("ApiKeySecretName");
    }

    [Fact]
    public void ARowThatDoesNotListChatIsRefused()
        => Refused(StubLanguageModelConfiguration.Carrying(["Vision"], ["High"])).ShouldContain("CapabilityNames");

    [Fact]
    public void ARowThatListsACapabilityNobodyDeclaredIsRefused()
        => Refused(StubLanguageModelConfiguration.Carrying(["Chat", "Telepathy"], ["High"])).ShouldContain("CapabilityNames");

    [Fact]
    public void ARowThatListsNoEffortOrAnEffortNobodyDeclaredIsRefused()
    {
        Refused(StubLanguageModelConfiguration.Carrying(["Chat"], [])).ShouldContain("ReasoningEffortNames");
        Refused(StubLanguageModelConfiguration.Carrying(["Chat"], ["Maximum"])).ShouldContain("ReasoningEffortNames");
    }
}
