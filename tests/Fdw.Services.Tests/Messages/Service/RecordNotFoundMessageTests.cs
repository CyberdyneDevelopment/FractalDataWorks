using System;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Messages.Service;

public class RecordNotFoundMessageTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DefaultConstructorInitializesWithCorrectProperties()
    {
        var message = new RecordNotFoundMessage();

        message.Id.ShouldBe(1002);
        message.Name.ShouldBe("RecordNotFound");
        message.Severity.ShouldBe(MessageSeverity.Warning);
        message.Code.ShouldBe("RECORD_NOT_FOUND");
        message.Message.ShouldContain("was not found");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithEntityTypeAndIdSetsFormattedMessage()
    {
        var entityId = Guid.NewGuid();
        var message = new RecordNotFoundMessage("Connection", entityId);

        message.Message.ShouldContain("Connection");
        message.Message.ShouldContain(entityId.ToString());
        message.Message.ShouldContain("was not found");
        message.Id.ShouldBe(1002);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorWithStringIdSetsFormattedMessage()
    {
        var message = new RecordNotFoundMessage("DataStore", "my-store-id");

        message.Message.ShouldContain("DataStore");
        message.Message.ShouldContain("my-store-id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageImplementsIServiceMessage()
    {
        new RecordNotFoundMessage().ShouldBeAssignableTo<IServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageInheritsFromServiceMessage()
    {
        new RecordNotFoundMessage().ShouldBeAssignableTo<ServiceMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MessageIsSealed()
    {
        typeof(RecordNotFoundMessage).IsSealed.ShouldBeTrue();
    }
}
