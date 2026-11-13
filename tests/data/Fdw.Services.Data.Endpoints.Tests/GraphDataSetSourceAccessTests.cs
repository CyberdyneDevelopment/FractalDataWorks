using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5295: the graph is answered only to a caller who may read every kind of source it reads; the caller who cannot be answered for is never allowed by default.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5295")]
public sealed class GraphDataSetSourceAccessTests
{
    private static GraphDataSetComponentConfiguration DataSetSource() => new() { Name = "Teams", ComponentType = "DataSetSource", DataSetName = "Teams" };

    private static GraphDataSetComponentConfiguration ContainerSource() => new()
    {
        Name = "Prices", ComponentType = "ContainerSource", DataStoreName = "Gas", ContainerName = "Prices",
    };

    private static GraphDataSetConfiguration Reading(params GraphDataSetComponentConfiguration[] components)
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices" };
        foreach (var component in components) graph.Components.Add(component);
        return graph;
    }

    private static Mock<IFrameworkAuthorizationService> Grants(params string[] resources)
    {
        var authorization = new Mock<IFrameworkAuthorizationService>();
        authorization.Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), "read", It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((IAuthenticationContext _, string resource, string _, System.Threading.CancellationToken _) =>
                GenericResult<bool>.Success(resources.Contains(resource, System.StringComparer.Ordinal)));
        return authorization;
    }

    [Fact]
    public async Task ACallerWhoMayReadEveryKindOfSourceMayReadTheGraph()
    {
        var answer = await new GraphDataSetSourceAccess(Grants("datasets", "datastores").Object).MayReadEvery(Reading(DataSetSource(), ContainerSource()), new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task ACallerWhoMayNotReadADataStoreMayNotReadAGraphThatReadsOne()
    {
        var answer = await new GraphDataSetSourceAccess(Grants("datasets").Object).MayReadEvery(Reading(DataSetSource(), ContainerSource()), new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task APermissionThatIsNotNeededIsNotAsked()
    {
        var authorization = Grants("datasets");
        var answer = await new GraphDataSetSourceAccess(authorization.Object).MayReadEvery(Reading(DataSetSource()), new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.Value.ShouldBeTrue();
        authorization.Verify(a => a.Authorize(It.IsAny<IAuthenticationContext>(), "datastores", It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AGraphThatReadsNothingAsksNothingAndMayBeRead()
    {
        var authorization = Grants();
        var answer = await new GraphDataSetSourceAccess(authorization.Object).MayReadEvery(Reading(), new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.Value.ShouldBeTrue();
        authorization.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnIntendedSourceIsAskedLikeAnyOther()
    {
        var graph = Reading();
        graph.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Gas", Ordinal = 0 });
        var answer = await new GraphDataSetSourceAccess(Grants("datasets").Object).MayReadEvery(graph, new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task AnAuthorizationThatCannotAnswerIsAFailureNotAnAllowance()
    {
        var authorization = new Mock<IFrameworkAuthorizationService>();
        authorization.Setup(a => a.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(new GenericMessage("the authorization store is down")));
        var answer = await new GraphDataSetSourceAccess(authorization.Object).MayReadEvery(Reading(DataSetSource()), new Mock<IAuthenticationContext>().Object, TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
    }
}
