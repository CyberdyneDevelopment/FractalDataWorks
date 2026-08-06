using Fdw.Services.SecretManagers.Abstractions.Secrets;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Secrets;

public class SecretTypesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllReturnsAllSecretTypes()
    {
        var all = SecretTypes.All();

        all.ShouldNotBeEmpty();
        all.ShouldContain(x => x.Name == "None");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsCorrectSecretType()
    {
        var result = SecretTypes.ById(0);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(0);
        result.Name.ShouldBe("None");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = SecretTypes.ById(99999);

        result.ShouldNotBeNull();
        result.ShouldBe(SecretTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnCorrectSecretType()
    {
        var result = SecretTypes.ByName("None");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("None");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameIsCaseSensitive()
    {
        var none = SecretTypes.ByName("None");
        none.ShouldNotBeNull();
        none.Name.ShouldBe("None");

        SecretTypes.ByName("none").ShouldBe(SecretTypes.NotFound);
        SecretTypes.ByName("NONE").ShouldBe(SecretTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = SecretTypes.ByName("UnknownSecretType");

        result.ShouldNotBeNull();
        result.ShouldBe(SecretTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = SecretTypes.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NonePropertyReturnsNoneSecretType()
    {
        var result = SecretTypes.None;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("None");
        result.Id.ShouldBe(0);
        result.RequiresSecureStorage.ShouldBeFalse();
    }
}
