using Fdw.Services.SecretManagers.Abstractions.Handlers;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Handlers;

public class EmptySecretManagerCommandHandlerTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var handler = new EmptySecretManagerCommandHandler();

        handler.Id.ShouldBe(0);
        handler.Name.ShouldBe("NotFound");
        handler.CommandTypeClass.ShouldBe(typeof(void));
        handler.ResultType.ShouldBe(typeof(void));
        handler.ExecuteFunc.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateReturnsFailure()
    {
        var handler = new EmptySecretManagerCommandHandler();
        var command = new Mock<ISecretManagerCommand>().Object;

        var result = handler.Validate(command);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages.ShouldContain(m => m.Message.Contains("Cannot execute Empty handler"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteFuncReturnsFailureForNullCommand()
    {
        var handler = new EmptySecretManagerCommandHandler();
        var context = new Mock<ISecretManagerExecutionContext>().Object;

        var result = await ((Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<Fdw.Results.IGenericResult<object?>>>)handler.ExecuteFunc)
            (null!, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages.ShouldContain(m => m.Message.Contains("No handler found"));
        result.Messages.ShouldContain(m => m.Message.Contains("null"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ExecuteFuncReturnsFailureWithCommandTypeName()
    {
        var handler = new EmptySecretManagerCommandHandler();
        var commandMock = new Mock<ISecretManagerCommand>();
        commandMock.Setup(c => c.CommandType).Returns("TestCommand");
        var context = new Mock<ISecretManagerExecutionContext>().Object;

        var result = await ((Func<ISecretManagerCommand, ISecretManagerExecutionContext, CancellationToken, Task<Fdw.Results.IGenericResult<object?>>>)handler.ExecuteFunc)
            (commandMock.Object, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages.ShouldContain(m => m.Message.Contains("TestCommand"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsISecretManagerCommandHandler()
    {
        var handler = new EmptySecretManagerCommandHandler();

        handler.ShouldBeAssignableTo<ISecretManagerCommandHandler>();
    }
}
