using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Http;
using Fdw.Services.Settings;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// FDW-5014: an HTTP connection's paging style is a choice only the caller makes, so a create, and a test of an unsaved configuration (which
/// fills and binds the body the same way), that leaves it out is refused, naming it. The style's Paging... settings travel in the
/// authentication dictionary beside the method's keys (FDW-4938): accepted and shown as they are, while a key neither expects is refused and
/// masked.
/// </summary>
public sealed class HttpPagingStyleCreateTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    [Trait("Priority", "P0")]
    public void AnHttpCreateThatStatesNoPagingStyleIsRefusedNamingIt()
    {
        var filled = Fill("""{ "baseUrl": "https://remote.test/", "protocol": "Rest", "authenticationType": "None", "lifetime": "Scoped" }""");

        filled.IsFailure.ShouldBeTrue();
        filled.CurrentMessage!.ShouldContain(nameof(HttpConnectionConfigurationBase.PagingStyle));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnHttpCreateThatStatesItsPagingStyleIsFilled()
    {
        var filled = Fill("""{ "baseUrl": "https://remote.test/", "protocol": "Rest", "authenticationType": "None", "lifetime": "Scoped", "pagingStyle": "OffsetLimit" }""");

        filled.IsSuccess.ShouldBeTrue(filled.CurrentMessage);
        filled.Value.GetProperty("pagingStyle").GetString().ShouldBe("OffsetLimit");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnHttpCreateThatSendsNullForThePagingStyleIsRefused()
    {
        var filled = Fill("""{ "baseUrl": "https://remote.test/", "protocol": "Rest", "authenticationType": "None", "lifetime": "Scoped", "pagingStyle": null }""");

        filled.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheStylesKeysAreAcceptedBesideTheMethodsAndAKeyNeitherExpectsIsRefused()
    {
        var behavior = SharedBehavior();

        var accepted = behavior.Bind(Json("""{ "additionalProperties": { "Username": "etl", "PagingOffsetParameter": "skip" } }"""), NullLogger.Instance);
        var refused = behavior.Bind(Json("""{ "additionalProperties": { "Username": "etl", "PagingCursor": "x" } }"""), NullLogger.Instance);

        accepted.IsSuccess.ShouldBeTrue(accepted.CurrentMessage);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage!.ShouldContain("PagingCursor");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheStylesKeysAreShownAsTheyAreAndAKeyNeitherExpectsIsMasked()
    {
        var stored = new MsSql.MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Server = "db", AuthenticationType = "SqlAuth" };
        stored.AdditionalProperties["Username"] = "etl";
        stored.AdditionalProperties["SecretKeyName"] = "db-password";
        stored.AdditionalProperties["PagingOffsetParameter"] = "skip";
        stored.AdditionalProperties["Stray"] = "leaked";

        var shown = SharedBehavior().Project(stored, NullLogger.Instance);

        shown.IsSuccess.ShouldBeTrue(shown.CurrentMessage);
        var entries = shown.Value!["additionalProperties"]!.AsObject();
        entries["PagingOffsetParameter"]!.GetValue<string>().ShouldBe("skip");
        entries["Username"]!.GetValue<string>().ShouldBe("etl");
        entries["SecretKeyName"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
        entries["Stray"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ABlankKeyNeitherOwnerExpectsStatesNothingAndIsNotRefused()
    {
        var behavior = SharedBehavior();

        var bound = behavior.Bind(Json("""{ "additionalProperties": { "Username": "etl", "PagingLimitParameter": "", "PagingCursor": null } }"""), NullLogger.Instance);

        bound.IsSuccess.ShouldBeTrue(bound.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AMergeThatBlanksTheOldSettingsIsNotRefused()
    {
        var stored = new MsSql.MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Server = "db", AuthenticationType = "SqlAuth" };
        stored.AdditionalProperties["Username"] = "etl";
        stored.AdditionalProperties["PagingStale"] = "top";

        var merged = SharedBehavior().Merge(Json("""{ "additionalProperties": { "PagingStale": "" } }"""), stored, NullLogger.Instance);

        merged.IsSuccess.ShouldBeTrue(merged.CurrentMessage);
    }

    private static Fdw.Results.IGenericResult<JsonElement> Fill(string body)
    {
        var behavior = new Mock<IConnectionEndpointBehavior>();
        behavior.SetupGet(b => b.ConfigurationType).Returns(typeof(HttpConnectionConfiguration));
        behavior.SetupGet(b => b.Aliases).Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var option = new Mock<IServiceType>();
        option.SetupGet(o => o.Name).Returns("Http");
        return ConnectionCreateDefaults.Fill(
            Json(body), behavior.Object, option.Object, new Lazy<IEffectiveSettingsProvider>(() => new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict).Object),
            Tenant, "remote", NullLogger.Instance);
    }

    // An authentication method that expects Username and SecretKeyName (a credential), and a second owner (the paging style) that expects
    // PagingOffsetParameter, as HttpConnectionType declares them with ConnectionAuthentication.SharedWith.
    private static ConnectionEndpointBehavior SharedBehavior()
    {
        string[] expected = ["Username", "SecretKeyName"];
        var method = new Mock<IConnectionAuthenticationType>();
        method.SetupGet(m => m.Name).Returns("SqlAuth");
        method.SetupGet(m => m.ExpectedProperties).Returns(expected);
        method.Setup(m => m.UnexpectedProperties(It.IsAny<IEnumerable<string>>()))
            .Returns<IEnumerable<string>>(names => names.Where(name => !expected.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList());
        method.Setup(m => m.MaskSecrets(It.IsAny<IReadOnlyDictionary<string, string?>>(), It.IsAny<string>()))
            .Returns<IReadOnlyDictionary<string, string?>, string>((values, mask) => values.ToDictionary(
                entry => entry.Key,
                entry => string.Equals(entry.Key, "Username", StringComparison.OrdinalIgnoreCase) ? entry.Value : mask,
                StringComparer.OrdinalIgnoreCase));

        var behavior = new ConnectionEndpointBehavior(typeof(MsSql.MsSqlConnectionConfiguration));
        behavior.Authentication(
            nameof(MsSql.MsSqlConnectionConfiguration.AdditionalProperties), "MsSql", _ => method.Object, _ => ["PagingOffsetParameter"]);
        return behavior;
    }

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }
}
