using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-3660: every protocol is reached through the adapter, so a Delete that states no rows (no filter, no soft delete, no
/// AllRows) is refused there and no HTTP DELETE goes out without a condition by accident.
/// </summary>
public sealed class HttpProtocolTranslatorAdapterTests
{
    private static (HttpProtocolTranslatorAdapter Adapter, Mock<IHttpProtocol> Protocol) Adapter()
    {
        var protocol = new Mock<IHttpProtocol>();
        protocol
            .Setup(p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<HttpRequestMessage>.Success(new HttpRequestMessage(HttpMethod.Delete, "/customers")));
        return (new HttpProtocolTranslatorAdapter(protocol.Object, default), protocol);
    }

    private static IStorageContainer Container()
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("customers");
        return container.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteThatStatesNoRowsIsRefusedBeforeTheProtocolBuildsARequest()
    {
        var (adapter, protocol) = Adapter();

        var result = await adapter.Translate(new DeleteCommand(), Container(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("AllRows()");
        protocol.Verify(
            p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteThatAsksForEveryRowReachesTheProtocol()
    {
        var (adapter, protocol) = Adapter();

        var result = await adapter.Translate(new DeleteCommand { AllRows = true }, Container(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        protocol.Verify(
            p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AQueryWithNoFilterIsNotADeleteAndReachesTheProtocol()
    {
        var (adapter, protocol) = Adapter();

        var result = await adapter.Translate(new QueryCommand<object>(), Container(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        protocol.Verify(
            p => p.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<HttpProtocolContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
