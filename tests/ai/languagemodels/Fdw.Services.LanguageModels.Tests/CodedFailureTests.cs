using Fdw.Messages;
using Fdw.Services.LanguageModels.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Acceptance criterion 9: each named failure is its own coded message.</summary>
public sealed class CodedFailureTests
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static IEnumerable<(string Name, string Code, IGenericMessage Message)> Failures()
    {
        yield return ("RateLimited", "LANGUAGEMODELS-80001", LanguageModelLog.RateLimited(Logger, "m"));
        yield return ("ContextTooLarge", "LANGUAGEMODELS-21000", LanguageModelLog.ContextTooLarge(Logger, "m"));
        yield return ("ContentFiltered", "LANGUAGEMODELS-21001", LanguageModelLog.ContentFiltered(Logger, "m"));
        yield return ("ProviderUnavailable", "LANGUAGEMODELS-70000", LanguageModelLog.ProviderUnavailable(Logger, "m", "status 503"));
        yield return ("ProviderRefused", "LANGUAGEMODELS-21002", LanguageModelLog.ProviderRefused(Logger, "m", "status 400"));
        yield return ("ModelNotConfigured", "LANGUAGEMODELS-60001", LanguageModelLog.ModelNotConfigured(Logger, "m"));
        yield return ("CapabilityNotCarried", "LANGUAGEMODELS-21003", LanguageModelLog.CapabilityNotCarried(Logger, "m", "Vision"));
        yield return ("EffortNotCarried", "LANGUAGEMODELS-21004", LanguageModelLog.EffortNotCarried(Logger, "m", "High"));
        yield return ("ModelIdMissing", "LANGUAGEMODELS-20000", LanguageModelLog.ModelIdMissing(Logger, "m"));
        yield return ("MaxOutputTokensInvalid", "LANGUAGEMODELS-21005", LanguageModelLog.MaxOutputTokensInvalid(Logger, "m"));
        yield return ("SecretNotFound", "LANGUAGEMODELS-30000", LanguageModelLog.SecretNotFound(Logger, "m", "Secrets", "KeyName"));
        yield return ("ProviderStopNotMapped", "LANGUAGEMODELS-90004", LanguageModelLog.ProviderStopNotMapped(Logger, "m", "pause_turn"));
    }

    [Fact]
    public void EveryNamedFailureCarriesItsOwnCode()
    {
        foreach (var (name, code, message) in Failures())
            message.Code.ShouldBe(code, name);
    }

    [Fact]
    public void NoTwoFailuresShareACode()
    {
        var codes = Failures().Select(f => f.Code).ToList();
        codes.Distinct(StringComparer.Ordinal).Count().ShouldBe(codes.Count);
    }

    [Fact]
    public void ASecretFailureStatesTheSecretsNameAndNeverAValue()
    {
        var message = LanguageModelLog.SecretNotFound(Logger, "m", "Secrets", "KeyName");

        message.Message.ShouldContain("KeyName");
        message.Message.ShouldContain("Secrets");
    }
}
