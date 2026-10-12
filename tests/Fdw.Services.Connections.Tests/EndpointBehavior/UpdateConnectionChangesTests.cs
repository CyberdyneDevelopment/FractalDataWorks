using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// Tests for <see cref="UpdateConnectionChanges"/> (FDW-1177): the one list of members an update may change decides whether a body
/// states anything, so a member can neither be missing from the rule nor be accepted and ignored.
/// </summary>
public sealed class UpdateConnectionChangesTests
{
    // One sample per member: a value that the body builder, the health-check copy, the schema discovery choice or the credential
    // store would act on. A member added to the request without a row here fails the first test below.
    private static readonly Dictionary<string, Action<UpdateConnectionRequest>> Samples = new(StringComparer.Ordinal)
    {
        [nameof(UpdateConnectionRequest.Server)] = request => request.Server = "db.example.com",
        [nameof(UpdateConnectionRequest.Port)] = request => request.Port = 1433,
        [nameof(UpdateConnectionRequest.Database)] = request => request.Database = "Orders",
        [nameof(UpdateConnectionRequest.AuthenticationType)] = request => request.AuthenticationType = "SqlAuth",
        [nameof(UpdateConnectionRequest.Authentication)] = request => request.Authentication = new Dictionary<string, string?> { ["UserName"] = "svc" },
        [nameof(UpdateConnectionRequest.TrustServerCertificate)] = request => request.TrustServerCertificate = true,
        [nameof(UpdateConnectionRequest.Encrypt)] = request => request.Encrypt = false,
        [nameof(UpdateConnectionRequest.BaseUrl)] = request => request.BaseUrl = "https://example.com/api",
        [nameof(UpdateConnectionRequest.Protocol)] = request => request.Protocol = "Rest",
        [nameof(UpdateConnectionRequest.TimeoutSeconds)] = request => request.TimeoutSeconds = 30,
        [nameof(UpdateConnectionRequest.Headers)] = request => request.Headers = new Dictionary<string, string?> { ["X-Team"] = "data" },
        [nameof(UpdateConnectionRequest.SecurityType)] = request => request.SecurityType = "Basic",
        [nameof(UpdateConnectionRequest.Security)] = request => request.Security = new Dictionary<string, string?> { ["UsernameSecretName"] = "u" },
        [nameof(UpdateConnectionRequest.UseMtls)] = request => request.UseMtls = true,
        [nameof(UpdateConnectionRequest.DefaultValuePolicy)] = request => request.DefaultValuePolicy = "Strict",
        [nameof(UpdateConnectionRequest.Configuration)] = request => request.Configuration = JsonDocument.Parse("{\"Root\":\"/data\"}").RootElement,
        [nameof(UpdateConnectionRequest.HealthCheckEnabled)] = request => request.HealthCheckEnabled = true,
        [nameof(UpdateConnectionRequest.HealthCheckOnStartup)] = request => request.HealthCheckOnStartup = true,
        [nameof(UpdateConnectionRequest.HealthCheckIntervalSeconds)] = request => request.HealthCheckIntervalSeconds = 60,
        [nameof(UpdateConnectionRequest.SchemaDiscovery)] = request => request.SchemaDiscovery = "None",
        [nameof(UpdateConnectionRequest.Credentials)] = request => request.Credentials = new Dictionary<string, string> { ["UsernameSecretName"] = "u", ["PasswordSecretName"] = "p" },
        [nameof(UpdateConnectionRequest.IsActive)] = request => request.IsActive = false,
    };

    public static IEnumerable<object[]> ChangeableNames() => UpdateConnectionChanges.Names.Select(name => new object[] { name });

    public static IEnumerable<object[]> MergedNames() =>
        UpdateConnectionChanges.Names
            .Where(name => string.Equals(UpdateConnectionChanges.ReachedBy(name), UpdateConnectionChanges.ReachesByMerge, StringComparison.Ordinal))
            .Select(name => new object[] { name });

    private static UpdateConnectionRequest Stating(string name)
    {
        var request = new UpdateConnectionRequest { Name = "Orders" };
        Samples[name](request);
        return request;
    }

    private static IEnumerable<string> DeclaredNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(property => property.Name);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void EveryMemberOfTheUpdateRequestIsOnTheListOrNamedUnchangeable()
    {
        var listed = UpdateConnectionChanges.Names.Concat(UpdateConnectionChanges.Unchangeable).ToList();

        DeclaredNames(typeof(UpdateConnectionRequest)).ToList().ShouldBe(listed, ignoreOrder: true);
        Samples.Keys.ToList().ShouldBe(listed, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(ChangeableNames))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void EachMemberAloneIsAChange(string name)
    {
        var request = Stating(name);

        UpdateConnectionChanges.Stated(request).ShouldBeTrue();
        UpdateConnectionChanges.Stated(request, name).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void CredentialsAloneAreAChangeSoAPasswordCanBeRotatedByItself()
    {
        var request = Stating(nameof(UpdateConnectionRequest.Credentials));

        UpdateConnectionChanges.Stated(request).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNothingIsNotAChangeAndTheRefusalNamesEveryMember()
    {
        UpdateConnectionChanges.Stated(new UpdateConnectionRequest { Name = "Orders" }).ShouldBeFalse();

        foreach (var name in UpdateConnectionChanges.Names.Concat(UpdateConnectionChanges.Unchangeable))
        {
            UpdateConnectionChanges.RefusalMessage.ShouldContain(name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AValueTheBodyBuilderDropsIsNotAChange()
    {
        var request = new UpdateConnectionRequest
        {
            Name = "Orders",
            Server = string.Empty,
            AuthenticationType = string.Empty,
            Authentication = new Dictionary<string, string?>(),
            Headers = new Dictionary<string, string?>(),
            Security = new Dictionary<string, string?>(),
            Configuration = JsonDocument.Parse("null").RootElement,
        };

        UpdateConnectionChanges.Stated(request).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void IsActiveIsStatedButNeverAChange()
    {
        var request = Stating(nameof(UpdateConnectionRequest.IsActive));

        UpdateConnectionChanges.Stated(request, nameof(UpdateConnectionRequest.IsActive)).ShouldBeTrue();
        UpdateConnectionChanges.Stated(request).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AMemberTheConnectionDoesNotStoreIsRefusedNamingIt()
    {
        var result = UpdateConnectionChanges.CheckUnchangeable(Stating(nameof(UpdateConnectionRequest.IsActive)), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(ConnectionsResultCodes.ByName("ConnectionMemberNotSettable"));
        result.Code!.Id.ShouldBe(20009);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNoUnsettableMemberIsNotRefused()
    {
        UpdateConnectionChanges.CheckUnchangeable(Stating(nameof(UpdateConnectionRequest.Server)), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(MergedNames))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AMemberThatReachesTheConnectionByMergeIsInTheJsonTheMergeReads(string name)
    {
        var body = ConnectionRequestBody.ForUpdate(Stating(name));

        body.ValueKind.ShouldBe(JsonValueKind.Object);
        body.EnumerateObject().Any().ShouldBeTrue($"{name} is stated but the merge would receive nothing for it");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheClientRequestCarriesTheSameMembersAsTheServerRequestBesidesItsRoutingDiscriminator()
    {
        var client = DeclaredNames(typeof(UpdateConnectionClientRequest)).Where(name => !string.Equals(name, "ServiceType", StringComparison.Ordinal));

        client.ToList().ShouldBe(DeclaredNames(typeof(UpdateConnectionRequest)).ToList(), ignoreOrder: true);
    }
}
