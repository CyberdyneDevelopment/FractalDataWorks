using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// Tests for SecretManagerTypeBase constructor and property assignment.
/// </summary>
public sealed class SecretManagerTypeBaseTests
{
    /// <summary>
    /// Concrete test subclass for exercising the SecretManagerTypeBase constructor.
    /// </summary>
    private sealed class TestSecretManagerType
        : SecretManagerTypeBase<ISecretManager, ISecretManagerServiceFactory<ISecretManager, SecretManagerConfiguration>, SecretManagerConfiguration>
    {

        public TestSecretManagerType(
            string[] supportedSecretStores,
            IReadOnlyList<string> supportedSecretTypes,
            bool supportsRotation = false,
            bool supportsVersioning = false,
            bool supportsSoftDelete = false,
            bool supportsAccessPolicies = false,
            int maxSecretSizeBytes = 10240,
            bool supportsBatchOperations = false,
            bool supportsExpiration = false,
            bool supportsTagging = false,
            int priority = 50)
            : base(
                name: "TestSecretManager",
                sectionName: "SecretManagers:Test",
                displayName: "Test Secret Manager",
                description: "A test secret manager type",
                supportedSecretStores: supportedSecretStores,
                supportedSecretTypes: supportedSecretTypes,
                supportsRotation: supportsRotation,
                supportsVersioning: supportsVersioning,
                supportsSoftDelete: supportsSoftDelete,
                supportsAccessPolicies: supportsAccessPolicies,
                maxSecretSizeBytes: maxSecretSizeBytes,
                supportsBatchOperations: supportsBatchOperations,
                supportsExpiration: supportsExpiration,
                supportsTagging: supportsTagging,
                priority: priority)
        {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
                return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange
        var stores = new[] { "AzureKeyVault", "HashiCorpVault" };
        var types = new List<string> { "Password", "Certificate", "Key" };

        // Act
        var sut = new TestSecretManagerType(
            supportedSecretStores: stores,
            supportedSecretTypes: types,
            supportsRotation: true,
            supportsVersioning: true,
            supportsSoftDelete: true,
            supportsAccessPolicies: true,
            maxSecretSizeBytes: 25600,
            supportsBatchOperations: true,
            supportsExpiration: true,
            supportsTagging: true,
            priority: 100);

        // Assert
        sut.SupportedSecretStores.ShouldBe(stores);
        sut.SupportedSecretTypes.ShouldBe(types);
        sut.SupportsRotation.ShouldBeTrue();
        sut.SupportsVersioning.ShouldBeTrue();
        sut.SupportsSoftDelete.ShouldBeTrue();
        sut.SupportsAccessPolicies.ShouldBeTrue();
        sut.MaxSecretSizeBytes.ShouldBe(25600);
        sut.SupportsBatchOperations.ShouldBeTrue();
        sut.SupportsExpiration.ShouldBeTrue();
        sut.SupportsTagging.ShouldBeTrue();
        sut.Priority.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithDefaultsUsesExpectedValues()
    {
        // Arrange & Act
        var sut = new TestSecretManagerType(
            supportedSecretStores: new[] { "Test" },
            supportedSecretTypes: new List<string> { "Password" });

        // Assert
        sut.SupportsBatchOperations.ShouldBeFalse();
        sut.SupportsExpiration.ShouldBeFalse();
        sut.SupportsTagging.ShouldBeFalse();
        sut.Priority.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorThrowsWhenSupportedSecretStoresIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestSecretManagerType(
                supportedSecretStores: null!,
                supportedSecretTypes: new List<string> { "Password" }));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorThrowsWhenSupportedSecretTypesIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new TestSecretManagerType(
                supportedSecretStores: new[] { "Test" },
                supportedSecretTypes: null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorAcceptsEmptySecretStores()
    {
        // Arrange & Act
        var sut = new TestSecretManagerType(
            supportedSecretStores: Array.Empty<string>(),
            supportedSecretTypes: new List<string> { "Password" });

        // Assert
        sut.SupportedSecretStores.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorAcceptsEmptySecretTypes()
    {
        // Arrange & Act
        var sut = new TestSecretManagerType(
            supportedSecretStores: new[] { "Test" },
            supportedSecretTypes: new List<string>());

        // Assert
        sut.SupportedSecretTypes.ShouldBeEmpty();
    }
}
