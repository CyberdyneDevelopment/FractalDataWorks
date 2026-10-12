using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1068: a connection that signs in as the host (Windows or Kerberos, a managed identity, the host's files) reaches
/// whatever the host reaches, including the platform's own databases, so only a platform administrator may create or
/// change one. Each authentication method states whether it acts as the host; a connection type that cannot tell is a
/// refusal, never a "no".
/// </summary>
public class HostIdentityGateTests
{
    private static readonly Dictionary<string, string?> NoValues = new();

    private static (Mock<IConnectionType> Type, IConnectionImplementationConfiguration Connection) Connection(IGenericResult<bool> answer)
    {
        var connection = new Mock<IConnectionImplementationConfiguration>();
        connection.SetupGet(c => c.Name).Returns("Warehouse");
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns("MsSql");
        type.Setup(t => t.UsesHostIdentity(connection.Object)).Returns(answer);
        return (type, connection.Object);
    }

    [Theory]
    [InlineData("WindowsAuth", true)]
    [InlineData("ManagedIdentity", true)]
    [InlineData("AzureCli", true)]
    [InlineData("SqlAuth", false)]
    public void EachMsSqlMethodStatesWhetherItActsAsTheHost(string method, bool actsAsHost)
        => MsSqlAuthenticationTypes.ByName(method).UsesHostIdentity(NoValues).ShouldBe(actsAsHost);

    [Theory]
    [InlineData("ServicePrincipal", false)]
    [InlineData("SPN", false)]
    [InlineData("ManagedIdentity", true)]
    [InlineData("Interactive", true)]
    [InlineData("Default", true)]
    public void EntraIdActsAsTheHostUnlessItSignsInAsAServicePrincipal(string mode, bool actsAsHost)
        => MsSqlAuthenticationTypes.ByName("EntraId")
            .UsesHostIdentity(new Dictionary<string, string?> { ["AzureAdMode"] = mode })
            .ShouldBe(actsAsHost);

    [Fact]
    public void EntraIdWithNoModeIsDefaultSignInWhichActsAsTheHost()
        => MsSqlAuthenticationTypes.ByName("EntraId").UsesHostIdentity(NoValues).ShouldBeTrue();

    [Fact]
    public void AConnectionThatActsAsTheHostIsRefusedToSomeoneWhoIsNotAPlatformAdministrator()
    {
        var (type, connection) = Connection(GenericResult<bool>.Success(true));

        var result = HostIdentityGate.Admit(type.Object, connection, callerIsPlatformAdmin: false, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(ConnectionsResultCodes.ByName("HostIdentityReservedToPlatform"));
    }

    [Fact]
    public void APlatformAdministratorMaySaveAConnectionThatActsAsTheHost()
    {
        var (type, connection) = Connection(GenericResult<bool>.Success(true));

        HostIdentityGate.Admit(type.Object, connection, callerIsPlatformAdmin: true, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AConnectionThatCarriesItsOwnCredentialIsAnyonesToSave()
    {
        var (type, connection) = Connection(GenericResult<bool>.Success(false));

        HostIdentityGate.Admit(type.Object, connection, callerIsPlatformAdmin: false, NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AConnectionTypeThatCannotTellIsARefusalEvenForAPlatformAdministrator()
    {
        var undetermined = GenericResult<bool>.Failure(
            ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
            ResultDetails.Create("name", "Warehouse", "reason", "it names no authentication method"));
        var (type, connection) = Connection(undetermined);

        var result = HostIdentityGate.Admit(type.Object, connection, callerIsPlatformAdmin: true, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(ConnectionsResultCodes.ByName("HostIdentityUndetermined"));
    }

    [Fact]
    public void TheRefusalIsA400WhoseDetailSaysWhyAndWhatToUseInstead()
    {
        var result = GenericResult.Failure(
            ConnectionsResultCodes.ByName("HostIdentityReservedToPlatform"),
            ResultDetails.Create("name", "Warehouse", "connectionType", "MsSql"));

        var (status, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(400);
        response.Detail.ShouldNotBeNull().ShouldContain("only a platform administrator");
        response.Detail.ShouldContain("carries its own credential");
    }
}
