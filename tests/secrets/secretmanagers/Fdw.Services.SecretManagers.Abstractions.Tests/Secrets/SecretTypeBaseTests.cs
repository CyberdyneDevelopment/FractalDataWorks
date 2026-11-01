using Fdw.Collections.Attributes;
using Fdw.Services.SecretManagers.Abstractions.Secrets;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Secrets;

public class SecretTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var secretType = new TestSecretType("Test", "Test description", true, "TestCategory");
        secretType.Name.ShouldBe("Test");
        secretType.Description.ShouldBe("Test description");
        secretType.RequiresSecureStorage.ShouldBeTrue();
        secretType.Category.ShouldBe("TestCategory");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorUsesDefaultCategoryWhenNull()
    {
        var secretType = new TestSecretType("Test", "Test description", false, null);

        secretType.Category.ShouldBe("Secret");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorUsesEmptyStringForNullDescription()
    {
        var secretType = new TestSecretType("Test", null!, false);

        secretType.Description.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecureStorageDefaultsToTrue()
    {
        var secretType = new TestSecretType("Test", "Description");

        secretType.RequiresSecureStorage.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecureStorageCanBeFalse()
    {
        var secretType = new TestSecretType("Test", "Description", false);

        secretType.RequiresSecureStorage.ShouldBeFalse();
    }

    [TypeOption(typeof(SecretTypes), "Test", RestrictToCurrentCompilation = true)]
    private sealed class TestSecretType : SecretTypeBase
    {
        public TestSecretType(string name, string description, bool requiresSecureStorage = true, string? category = null)
            : base(name, description, requiresSecureStorage, category)
        {
        }
    }
}
