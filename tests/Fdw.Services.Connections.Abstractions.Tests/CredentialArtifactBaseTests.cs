using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for CredentialArtifactBase via concrete test implementation.
/// </summary>
public class CredentialArtifactBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestCredentialArtifact : CredentialArtifactBase
    {
        public TestCredentialArtifact(string artifactType)
        {
            ArtifactTypeValue = artifactType;
        }

        public string ArtifactTypeValue { get; }

        public override string ArtifactType => ArtifactTypeValue;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ArtifactTypeReturnsCorrectValue()
    {
        // Arrange
        var expectedType = "TestArtifact";

        // Act
        var artifact = new TestCredentialArtifact(expectedType);

        // Assert
        artifact.ArtifactType.ShouldBe(expectedType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ImplementsICredentialArtifact()
    {
        // Arrange
        var artifact = new TestCredentialArtifact("Test");

        // Assert
        artifact.ShouldBeAssignableTo<ICredentialArtifact>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DifferentArtifactTypesAreDistinct()
    {
        // Arrange
        var artifact1 = new TestCredentialArtifact("Type1");
        var artifact2 = new TestCredentialArtifact("Type2");

        // Assert
        artifact1.ArtifactType.ShouldNotBe(artifact2.ArtifactType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ArtifactTypeCanBeEmptyString()
    {
        // Act
        var artifact = new TestCredentialArtifact(string.Empty);

        // Assert
        artifact.ArtifactType.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BaseClassIsAbstract()
    {
        // Act
        var type = typeof(CredentialArtifactBase);

        // Assert
        type.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ArtifactTypePropertyIsAbstract()
    {
        // Act
        var type = typeof(CredentialArtifactBase);
        var property = type.GetProperty(nameof(CredentialArtifactBase.ArtifactType));

        // Assert
        property.ShouldNotBeNull();
        property.GetGetMethod()?.IsAbstract.ShouldBeTrue();
    }
}
