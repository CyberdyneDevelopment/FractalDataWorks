using System;
using System.Linq;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerThrowsArgumentNullExceptionForNullServices()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ServiceCollectionExtensions.AddRoslynCommandHandler(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerRegistersTranslatorRegistryAsSingleton()
    {
        // Arrange — the registry requires a real ILoggerFactory, so a host must have added logging.
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddRoslynCommandHandler();
        var provider = services.BuildServiceProvider();

        // Assert
        var registry1 = provider.GetRequiredService<ITranslatorRegistry>();
        var registry2 = provider.GetRequiredService<ITranslatorRegistry>();
        registry1.ShouldBeSameAs(registry2);
        registry1.ShouldBeOfType<TranslatorRegistry>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerDoesNotOverwriteExistingTranslatorRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingRegistry = new TranslatorRegistry(NullLoggerFactory.Instance);
        services.AddSingleton<ITranslatorRegistry>(existingRegistry);

        // Act
        services.AddRoslynCommandHandler();
        var provider = services.BuildServiceProvider();

        // Assert — TryAddSingleton must not clobber the pre-registered instance
        provider.GetRequiredService<ITranslatorRegistry>().ShouldBeSameAs(existingRegistry);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerWithConfigureThrowsArgumentNullExceptionForNullServices()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => ServiceCollectionExtensions.AddRoslynCommandHandler(null!, _ => { }));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerWithConfigureThrowsArgumentNullExceptionForNullConfigureAction()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => services.AddRoslynCommandHandler((Action<ITranslatorRegistry>)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerWithConfigureInvokesConfigureActionOnResolvedRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));

        // Act
        services.AddRoslynCommandHandler(registry => registry.Register(translatorMock.Object));
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ITranslatorRegistry>();

        // Assert
        registry.GetTranslator(typeof(FakeRoslynCommand)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResolvingTheRegistryWithoutLoggingFailsRatherThanProducingSilentTranslators()
    {
        // A host that never added logging used to get a registry that handed out translators with a
        // NullLogger — a server that looks wired, runs every command, and reports nothing. Failing at
        // resolution is the whole point: the alternative is undiagnosable by construction.
        var services = new ServiceCollection();
        services.AddRoslynCommandHandler();

        Should.Throw<InvalidOperationException>(
            () => services.BuildServiceProvider().GetRequiredService<ITranslatorRegistry>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddRoslynCommandHandlerRegistersHandlerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRoslynCommandHandler();

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRoslynCommandHandler));

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(RoslynCommandHandler));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AddTranslatorThrowsArgumentNullExceptionForNullServices()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => ServiceCollectionExtensions.AddTranslator<FakeRoslynCommandTranslator>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AddTranslatorRegistersBothTranslatorInterfaceAndConcreteType()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTranslator<FakeRoslynCommandTranslator>();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IRoslynCommandTranslator>().ShouldBeOfType<FakeRoslynCommandTranslator>();
        provider.GetRequiredService<FakeRoslynCommandTranslator>().ShouldNotBeNull();
    }
}
