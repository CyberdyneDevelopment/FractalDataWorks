using System;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>
/// A failed result always carries a message, and <see cref="FailureMessageExtensions.FailureReason"/> reads it, so no reader
/// supplies a default message (FDW-3580).
/// </summary>
public class FailureMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureReasonIsTheLastMessageOfAFailureWithItsCode()
    {
        var result = GenericResult.Failure(
            new GenericMessage(MessageSeverity.Error, "the connection 'Main' was refused", "CONN-1"),
            new GenericMessage(MessageSeverity.Error, "the data store could not be read", "DATA-2"));

        var reason = result.FailureReason();

        reason.Message.ShouldBe("the data store could not be read");
        reason.Code.ShouldBe("DATA-2");
        reason.Message.ShouldBe(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FailureReasonOfASuccessThrows()
    {
        var result = GenericResult<int>.Success(3);

        Should.Throw<InvalidOperationException>(() => result.FailureReason().Message);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AFailureWithoutAMessageCarriesTheNamedUnstatedFailure()
    {
        GenericResult.Failure().FailureReason().Code.ShouldBe(UnstatedFailure.Code);
        GenericResult<int>.Failure(Array.Empty<IGenericMessage>()).FailureReason().Code.ShouldBe(UnstatedFailure.Code);
        GenericResult.Failure(new GenericMessage(MessageSeverity.Error, string.Empty, "EMPTY-1")).FailureReason().Code.ShouldBe(UnstatedFailure.Code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NoValueReasonGivesTheFailureOrTheNamedMissingValue()
    {
        var failed = GenericResult<string>.Failure(new GenericMessage(MessageSeverity.Error, "store unreachable", "DATA-21001"));
        failed.NoValueReason().Code.ShouldBe("DATA-21001");

        var empty = GenericResult<string?>.Success(null);
        empty.NoValueReason().Code.ShouldBe(MissingValue.Code);
        empty.NoValueReason().Message.ShouldContain("String");

        Should.Throw<InvalidOperationException>(() => GenericResult<string>.Success("x").NoValueReason());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASuccessWithoutAMessageIsStillAllowed()
    {
        var result = GenericResult<int>.Success(3);

        result.IsSuccess.ShouldBeTrue();
        result.CurrentMessage.ShouldBeNull();
    }
}
