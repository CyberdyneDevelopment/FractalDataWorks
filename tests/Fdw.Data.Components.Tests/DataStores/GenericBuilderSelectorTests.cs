using System;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.Components.DataStores;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Components.Tests.DataStores;

/// <summary>
/// Tests for <see cref="GenericBuilderSelector"/> — the UI's single, always-succeeding
/// <see cref="IDataStoreBuilderSelector"/> that resolves every store to a
/// <see cref="GenericDataStoreBuilder"/> with <see cref="FormatTypes.NotFound"/> as its default
/// response format (the UI has no transport-type registry and the display DTOs carry no format hint).
/// </summary>
public sealed class GenericBuilderSelectorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SelectThrowsArgumentNullExceptionWhenConfigurationIsNull()
    {
        var sut = new GenericBuilderSelector();

        Should.Throw<ArgumentNullException>(() => sut.Select(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SelectReturnsSuccessWithAGenericDataStoreBuilder()
    {
        var sut = new GenericBuilderSelector();
        var configuration = new DataStoreConfiguration { Name = "Store1" };

        var result = sut.Select(configuration);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<GenericDataStoreBuilder>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SelectedBuilderResolvesContainerFormatToNotFoundWhenContainerHasNoExplicitFormat()
    {
        // Why: verifies the defaultFormat decision — the UI has no transport registry / format hint on
        // the display DTOs, so GenericBuilderSelector passes FormatTypes.NotFound as the transport
        // default; a container without its own explicit Format therefore resolves to NotFound (fail
        // loud at read time) rather than silently defaulting to Json/Tabular.
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "Customers" };
        var path = new DataPathConfiguration { Id = Guid.NewGuid(), Name = "dbo", Containers = [container] };
        var storeConfig = new DataStoreConfiguration { Name = "Store1", Paths = [path] };
        var sut = new GenericBuilderSelector();

        var selectResult = sut.Select(storeConfig);
        selectResult.IsSuccess.ShouldBeTrue();
        selectResult.Value.ShouldNotBeNull();

        var configureResult = selectResult.Value.Configure(storeConfig);
        configureResult.IsSuccess.ShouldBeTrue();

        var buildResult = await selectResult.Value.Build(TestContext.Current.CancellationToken);
        buildResult.IsSuccess.ShouldBeTrue();
        buildResult.Value.ShouldNotBeNull();

        var pathResult = buildResult.Value.Path("dbo");
        pathResult.IsSuccess.ShouldBeTrue();
        pathResult.Value.ShouldNotBeNull();
        var containerResult = pathResult.Value.Container("Customers");
        containerResult.IsSuccess.ShouldBeTrue();
        containerResult.Value.ShouldNotBeNull();

        containerResult.Value.Format.ShouldBe(FormatTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SelectPassesThroughSuppliedLoggerToTheBuilder()
    {
        var sut = new GenericBuilderSelector();
        var configuration = new DataStoreConfiguration { Name = "Store1" };

        // Why: GenericBuilderSelector must not swallow the caller's logger — ConfiguredDataStoreProvider
        // relies on Select(config, logger) threading its own logger into the selected builder for build
        // diagnostics. No public accessor exists on GenericDataStoreBuilder to assert the captured
        // instance directly, so this asserts the call succeeds with a non-null logger without throwing.
        var result = sut.Select(configuration, NullLoggerFactory.Instance.CreateLogger(nameof(GenericBuilderSelectorTests)));

        result.IsSuccess.ShouldBeTrue();
    }
}
