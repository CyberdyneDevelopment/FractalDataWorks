using Fdw.Services.SecretManagers.Abstractions.Secrets;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Secrets;

public class NoneSecretTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecureStorageIsFalse()
    {
        var secretType = new NoneSecretType();

        secretType.RequiresSecureStorage.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ImplementsISecretType()
    {
        var secretType = new NoneSecretType();

        secretType.ShouldBeAssignableTo<ISecretType>();
    }
}
