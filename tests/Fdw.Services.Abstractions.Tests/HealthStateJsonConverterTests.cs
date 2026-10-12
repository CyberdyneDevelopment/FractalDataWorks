using System;
using System.Text.Json;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Abstractions.Health.Converters;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="HealthStateJsonConverter"/>.
/// Verifies round-trip serialization, bare-string token deserialization,
/// full-object deserialization, and fail-loud behavior on unknown names.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class HealthStateJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new HealthStateJsonConverter() },
    };

    // ── Round-trip: serialize → deserialize returns the same state ─────────

    [Fact]
    [Trait("Priority", "P0")]
    public void RoundTripHealthyStateReturnsHealthy()
    {
        // Arrange
        var original = HealthStates.ByName("Healthy");

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe(original.Name);
        restored.Id.ShouldBe(original.Id);
        restored.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void RoundTripUnhealthyStateReturnsUnhealthy()
    {
        // Arrange
        var original = HealthStates.ByName("Unhealthy");

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe("Unhealthy");
        restored.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void RoundTripDegradedStateReturnsDegraded()
    {
        // Arrange
        var original = HealthStates.ByName("Degraded");

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        restored.ShouldNotBeNull();
        restored.Name.ShouldBe("Degraded");
        restored.IsHealthy.ShouldBeFalse();
    }

    // ── Write: serializes the state as its bare name string ────────────────

    [Fact]
    [Trait("Priority", "P0")]
    public void SerializeWritesNameAsStringToken()
    {
        // Arrange
        var state = HealthStates.ByName("Healthy");

        // Act
        var json = JsonSerializer.Serialize(state, Options);

        // Assert
        json.ShouldBe("\"Healthy\"");
    }

    // ── Read: bare string token ─────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeBareStringTokenHealthy()
    {
        // Act
        var result = JsonSerializer.Deserialize<IHealthState>("\"Healthy\"", Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Healthy");
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeBareStringTokenUnhealthy()
    {
        // Act
        var result = JsonSerializer.Deserialize<IHealthState>("\"Unhealthy\"", Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Unhealthy");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeBareStringTokenDegraded()
    {
        // Act
        var result = JsonSerializer.Deserialize<IHealthState>("\"Degraded\"", Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Degraded");
        result.IsHealthy.ShouldBeFalse();
    }

    // ── Read: full object shape {"id":1,"name":"Healthy",...} ──────────────

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeFullObjectShapeResolvesHealthy()
    {
        // Arrange
        const string json = "{\"id\":1,\"name\":\"Healthy\",\"isHealthy\":true}";

        // Act
        var result = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Healthy");
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeFullObjectShapeResolvesUnhealthy()
    {
        // Arrange
        const string json = "{\"id\":2,\"name\":\"Unhealthy\",\"isHealthy\":false}";

        // Act
        var result = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Unhealthy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeObjectWithExtraPropertiesStillResolvesName()
    {
        // Arrange
        const string json = "{\"extra\":\"value\",\"name\":\"Degraded\",\"code\":\"foo\"}";

        // Act
        var result = JsonSerializer.Deserialize<IHealthState>(json, Options);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Degraded");
    }

    // ── Read: null token returns null ───────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void DeserializeNullTokenReturnsNull()
    {
        // Act
        var result = JsonSerializer.Deserialize<IHealthState>("null", Options);

        // Assert
        result.ShouldBeNull();
    }

    // ── Read: unknown name throws JsonException (fail loud) ────────────────

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeUnknownNameThrowsJsonException()
    {
        // Act & Assert
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<IHealthState>("\"UnknownStateName\"", Options));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeUnknownNameInObjectThrowsJsonException()
    {
        // Arrange
        const string json = "{\"id\":99,\"name\":\"UnknownStateName\"}";

        // Act & Assert
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<IHealthState>(json, Options));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void DeserializeWrongTokenTypeThrowsJsonException()
    {
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<IHealthState>("42", Options));
    }

    // ── Write: a null value serializes as JSON null ───────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SerializeNullValueWritesJsonNull()
    {
        // Arrange
        IHealthState? nullState = null;

        // Act
        var json = JsonSerializer.Serialize(nullState, Options);

        // Assert
        json.ShouldBe("null");
    }
}
