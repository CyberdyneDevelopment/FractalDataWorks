using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Settings;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// A connection create fills each value the request leaves out from the connection type that carries it, keeps every value the
/// request states, and refuses a request that leaves out a choice only the caller makes (FDW-3823: "the thing you select carries it").
/// </summary>
public class ConnectionCreateDefaultsTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static IConnectionEndpointBehavior Behavior(IReadOnlyDictionary<string, string>? aliases = null)
    {
        var behavior = new Mock<IConnectionEndpointBehavior>();
        behavior.SetupGet(b => b.ConfigurationType).Returns(typeof(DefaultsTestConfiguration));
        behavior.SetupGet(b => b.Aliases).Returns(aliases ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        return behavior.Object;
    }

    private static IServiceType ConnectionType()
    {
        var option = new Mock<IServiceType>();
        option.SetupGet(o => o.Name).Returns("DefaultsTest");
        option.SetupGet(o => o.Category).Returns("DefaultsTestConnection");
        option.As<IDeclaresDefaults>().SetupGet(o => o.DeclaredDefaults).Returns(
            new Dictionary<string, object>(StringComparer.Ordinal) { ["Port"] = 1433, ["Encrypt"] = true, ["CommandTimeoutSeconds"] = 30 });
        return option.Object;
    }

    private static Lazy<IEffectiveSettingsProvider> Settings() => new(() => new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict).Object);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    [Trait("Priority", "P0")]
    public void AnOmittedValueTakesTheTypesDeclaredDefault()
    {
        var filled = ConnectionCreateDefaults.Fill(
            Json("""{ "host": "db.example", "database": "Ops", "trustServerCertificate": false }"""), Behavior(), ConnectionType(), Settings(), Tenant, "ops",
            NullLogger.Instance);

        filled.IsSuccess.ShouldBeTrue();
        filled.Value.GetProperty("port").GetInt32().ShouldBe(1433);
        filled.Value.GetProperty("encrypt").GetBoolean().ShouldBeTrue();
        filled.Value.GetProperty("host").GetString().ShouldBe("db.example");
        filled.Value.GetProperty("database").GetString().ShouldBe("Ops");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AStatedValueIsKeptEvenWhenItIsZeroOrFalse()
    {
        var filled = ConnectionCreateDefaults.Fill(
            Json("""{ "Host": "mine", "port": 0, "encrypt": false, "trustServerCertificate": true }"""), Behavior(), ConnectionType(), Settings(), Tenant,
            "ops", NullLogger.Instance);

        filled.IsSuccess.ShouldBeTrue();
        filled.Value.GetProperty("Host").GetString().ShouldBe("mine");
        filled.Value.GetProperty("port").GetInt32().ShouldBe(0);
        filled.Value.GetProperty("encrypt").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AChoiceSentUnderItsAliasIsStated()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["trust"] = nameof(DefaultsTestConfiguration.TrustServerCertificate) };

        var filled = ConnectionCreateDefaults.Fill(Json("""{ "trust": true }"""), Behavior(aliases), ConnectionType(), Settings(), Tenant, "ops", NullLogger.Instance);

        filled.IsSuccess.ShouldBeTrue();
        filled.Value.TryGetProperty("trustServerCertificate", out _).ShouldBeFalse();
        filled.Value.GetProperty("trust").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnOmittedCallerChoiceRefusesTheCreate()
    {
        var filled = ConnectionCreateDefaults.Fill(Json("""{ "host": "db.example" }"""), Behavior(), ConnectionType(), Settings(), Tenant, "ops", NullLogger.Instance);

        filled.IsSuccess.ShouldBeFalse();
        filled.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(DefaultsTestConfiguration.TrustServerCertificate));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnOmittedTimeoutIsLeftEmptySoTheTypesDefaultReachesItWhenItIsBuilt()
    {
        // FDW-4904: a timeout is resolved where the connection is built, so a later registration override reaches it; Port is still filled.
        var filled = ConnectionCreateDefaults.Fill(
            Json("""{ "host": "db.example", "trustServerCertificate": false }"""), Behavior(), ConnectionType(), Settings(), Tenant, "ops", NullLogger.Instance);

        filled.IsSuccess.ShouldBeTrue();
        filled.Value.TryGetProperty("commandTimeoutSeconds", out _).ShouldBeFalse();
        filled.Value.GetProperty("port").GetInt32().ShouldBe(1433);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AStatedTimeoutIsKept()
    {
        var filled = ConnectionCreateDefaults.Fill(
            Json("""{ "host": "db.example", "trustServerCertificate": false, "commandTimeoutSeconds": 600 }"""), Behavior(), ConnectionType(), Settings(), Tenant,
            "ops", NullLogger.Instance);

        filled.IsSuccess.ShouldBeTrue();
        filled.Value.GetProperty("commandTimeoutSeconds").GetInt32().ShouldBe(600);
    }
}
