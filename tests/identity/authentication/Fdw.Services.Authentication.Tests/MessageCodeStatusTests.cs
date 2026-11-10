using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests;

/// <summary>
/// FDW-4843: a code's category is its number divided by 10000, and the category chooses the HTTP status. The sign-in refusals sat
/// in Internal (500) and the host's own authentication settings in Dependency (502, retried). A refusal is now the caller's 4xx and a
/// settings fault the host's 500. The redemption and resume refusals are each one refusal on purpose (missing, expired and used
/// read the same, or the answer would say the code was real), so each keeps one code and one status.
/// </summary>
public sealed class MessageCodeStatusTests
{
    private static int StatusOf(IGenericMessage message)
    {
        var code = message.Code.ShouldNotBeNull();
        return ResultCategories.ById(int.Parse(code[(code.LastIndexOf('-') + 1)..], NumberStyles.None, CultureInfo.InvariantCulture) / 10000).HttpStatus;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ASignInCodeThatRedeemsNothingAnswersUnauthorized()
        => StatusOf(DatabaseAuthorizationCodeStoreLog.NotRedeemable(NullLogger<DatabaseAuthorizationCodeStore>.Instance)).ShouldBe(401);

    [Fact]
    [Trait("Priority", "P1")]
    public void AResumeTokenThatResumesNothingAnswersUnauthorizedFromEitherStore()
    {
        StatusOf(DatabaseExecutionStoreLog.NotResumable(NullLogger<DatabaseExecutionStore>.Instance)).ShouldBe(401);
        StatusOf(ExecutionStoreLog.NotResumable(NullLogger<InMemoryExecutionStore>.Instance)).ShouldBe(401);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AFlowNobodyConfiguredAnswersNotFound()
        => StatusOf(FlowProviderLog.NoSuchFlow(NullLogger<AuthenticationFlowProvider>.Instance, "ghost", "password")).ShouldBe(404);

    [Fact]
    [Trait("Priority", "P1")]
    public void AnAuthenticationEntryWithoutANameAnswersServerError()
        => StatusOf(AuthenticationValidationLog.EntryMissingName(NullLogger.Instance, "AuthenticationServices:0")).ShouldBe(500);

    [Fact]
    [Trait("Priority", "P1")]
    public void ARejectedTokenIsAnAuthenticationFailure()
        => StatusOf(AuthenticationValidationLog.TokenRejected(NullLogger.Instance, "Local", "expired")).ShouldBe(401);
}
