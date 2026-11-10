using System;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Results.Tests;

/// <summary>
/// FDW-4851: <c>Caused</c> is the failure of a caller that received a failure. It is the cause whole, with the caller's message last,
/// so the cause's messages, code, chain and details survive, and what classifies the failure is still the cause's.
/// </summary>
public sealed class GenericResultCausedTests
{
    private static readonly GenericMessage Context = new(MessageSeverity.Error, "the key could not be revoked", "USERSENDPOINTS-91045", "Test");

    private static Mock<IResultCode> FailingCode(string code)
    {
        var severity = new Mock<IResultSeverity>();
        severity.Setup(s => s.IsSuccess).Returns(false);
        var resultCode = new Mock<IResultCode>();
        resultCode.Setup(c => c.Severity).Returns(severity.Object);
        resultCode.Setup(c => c.Code).Returns(code);
        resultCode.Setup(c => c.Domain).Returns("Test");
        resultCode.Setup(c => c.FormatMessage(It.IsAny<IResultDetails?>())).Returns("formatted");
        return resultCode;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheCausesMessagesSurviveInOrderAndTheCallersMessageIsLast()
    {
        var cause = GenericResult.Failure(
            new GenericMessage(MessageSeverity.Error, "vault unreachable", "TEST-71000", "Test"),
            new GenericMessage(MessageSeverity.Error, "the store did not answer", "TEST-71001", "Test"));

        var caused = GenericResult.Caused(cause, Context);

        caused.IsFailure.ShouldBeTrue();
        caused.Messages.Select(message => message.Message).ShouldBe(["vault unreachable", "the store did not answer", "the key could not be revoked"]);
        caused.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("TEST-71000");
        cause.Messages.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheCausesCodeChainAndDetailsSurvive()
    {
        var inner = GenericResult.Failure(new GenericMessage(MessageSeverity.Error, "row missing", "TEST-31000", "Test"));
        var cause = GenericResult.WithDetails(GenericResult.Chain(FailingCode("TEST-71002").Object, inner), ResultDetails.Create("connectionId", "abc"));

        var caused = GenericResult.Caused(cause, Context);

        caused.Code.ShouldBeSameAs(cause.Code);
        caused.InnerResult.ShouldBeSameAs(inner);
        caused.Details.ShouldNotBeNull().Data["connectionId"].ShouldBe("abc");
        caused.Messages[^1].Code.ShouldBe("USERSENDPOINTS-91045");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheCallersMessageClassifiesTheFailureOnlyWhenTheCauseCarriesNoCode()
    {
        var caused = GenericResult.Caused(GenericResult.Failure(new GenericMessage("store failed")), Context);

        caused.Code.ShouldBeNull();
        caused.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("USERSENDPOINTS-91045");
        caused.Messages[0].Message.ShouldBe("store failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ASuccessIsRefusedBecauseItHasNoFailureToCarry()
    {
        Should.Throw<ArgumentException>(() => GenericResult.Caused(GenericResult.Success(), Context));
    }
}
