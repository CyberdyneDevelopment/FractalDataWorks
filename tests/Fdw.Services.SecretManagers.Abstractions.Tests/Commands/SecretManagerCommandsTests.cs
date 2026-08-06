using Fdw.Services.SecretManagers.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Commands;

public class SecretManagerCommandsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllReturnsCommandCollection()
    {
        var all = SecretManagerCommands.All();

        all.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = SecretManagerCommands.ById(99999);

        result.ShouldNotBeNull();
        result.ShouldBe(SecretManagerCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = SecretManagerCommands.ByName("UnknownCommand");

        result.ShouldNotBeNull();
        result.ShouldBe(SecretManagerCommands.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = SecretManagerCommands.NotFound;

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe(string.Empty);
    }
}
