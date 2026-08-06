using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Notifications.Tests.Services;

/// <summary>
/// Tests for <see cref="SqlUserNotificationPreferenceService"/>: the DataGateway-backed
/// query/upsert logic behind <see cref="IUserNotificationPreferenceService"/> — natural-key
/// matching (NotificationType + Channel, case-insensitive), the insert-vs-update branch, write
/// failure propagation, exception handling, and cancellation semantics.
/// </summary>
public sealed class SqlUserNotificationPreferenceServiceTests
{
    private static UserNotificationPreferenceConfiguration Row(
        Guid userId, string notificationType, string channel, bool isEnabled) =>
        new()
        {
            UserId = userId,
            NotificationType = notificationType,
            Channel = channel,
            IsEnabled = isEnabled,
            IsCurrent = true,
            IsDeleted = false,
        };

    private static void SetupQuery(
        Mock<IDataGateway> gateway,
        IGenericResult<IEnumerable<UserNotificationPreferenceConfiguration>> result)
    {
        gateway.Setup(g => g.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static void SetupQuerySequence(
        Mock<IDataGateway> gateway,
        params IGenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>[] results)
    {
        var sequence = gateway.SetupSequence(g => g.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()));
        foreach (var result in results)
        {
            sequence = sequence.ReturnsAsync(result);
        }
    }

    private static void SetupWrite(Mock<IDataGateway> gateway, IGenericResult<int> result)
    {
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static void SetupWriteCapturingCommandType(Mock<IDataGateway> gateway, IGenericResult<int> result, List<string> capturedCommandTypes)
    {
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((cmd, _, _) => capturedCommandTypes.Add(cmd.CommandType))
            .ReturnsAsync(result);
    }

    private static SqlUserNotificationPreferenceService CreateService(Mock<IDataGateway> gateway) =>
        new(new Lazy<IDataGateway>(gateway.Object), NullLogger<SqlUserNotificationPreferenceService>.Instance);

    // ──────────────────────────────────────────── Constructor ───────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsArgumentNullExceptionWhenDataGatewayIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SqlUserNotificationPreferenceService(null!, NullLogger<SqlUserNotificationPreferenceService>.Instance));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSucceedsWhenLoggerIsNull()
    {
        // Arrange
        var gateway = new Mock<IDataGateway>();

        // Act
        var sut = new SqlUserNotificationPreferenceService(new Lazy<IDataGateway>(gateway.Object), null);

        // Assert
        sut.ShouldNotBeNull();
    }

    // ─────────────────────────────────────────── GetPreferences ─────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPreferencesReturnsMappedPreferencesWhenTheQuerySucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
        [
            Row(userId, "PipelineFailure", "Email", true),
            Row(userId, "ScheduleMissed", "InApp", false),
        ]));
        var sut = CreateService(gateway);

        // Act
        var result = await sut.GetPreferences(userId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value.Any(p => p.NotificationType == "PipelineFailure" && p.Channel == "Email" && p.IsEnabled).ShouldBeTrue();
        result.Value.Any(p => p.NotificationType == "ScheduleMissed" && p.Channel == "InApp" && !p.IsEnabled).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPreferencesReturnsEmptyListWhenNoRowsMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            Array.Empty<UserNotificationPreferenceConfiguration>()));
        var sut = CreateService(gateway);

        // Act
        var result = await sut.GetPreferences(userId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPreferencesReturnsFailureWhenTheQueryFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Failure(
            Mock.Of<Fdw.Messages.IGenericMessage>(m => m.Message == "connection lost")));
        var sut = CreateService(gateway);

        // Act
        var result = await sut.GetPreferences(userId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to query notification preferences");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPreferencesReturnsFailureWhenTheGatewayThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateService(gateway);

        // Act
        var result = await sut.GetPreferences(userId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to query notification preferences");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetPreferencesRethrowsOperationCanceledExceptionWithoutWrappingIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = CreateService(gateway);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.GetPreferences(userId, TestContext.Current.CancellationToken));
    }

    // ─────────────────────────────────────────── SavePreferences ────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesThrowsArgumentNullExceptionWhenPreferencesIsNull()
    {
        // Arrange
        var sut = CreateService(new Mock<IDataGateway>());

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SavePreferences(Guid.NewGuid(), null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesReturnsFailureAndNeverWritesWhenTheInitialQueryFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Failure(
            Mock.Of<Fdw.Messages.IGenericMessage>(m => m.Message == "connection lost")));
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to query notification preferences");
        gateway.Verify(
            g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesInsertsWhenNoExistingRowMatchesAndReturnsTheRoundTrippedResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuerySequence(
            gateway,
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(Array.Empty<UserNotificationPreferenceConfiguration>()),
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            [
                Row(userId, "PipelineFailure", "Email", true),
            ]));
        var capturedCommandTypes = new List<string>();
        SetupWriteCapturingCommandType(gateway, GenericResult<int>.Success(1), capturedCommandTypes);
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert — round-tripped from the second query, not the echoed input.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].NotificationType.ShouldBe("PipelineFailure");
        capturedCommandTypes.ShouldHaveSingleItem();
        capturedCommandTypes[0].ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesUpdatesWhenAnExistingRowMatchesNotificationTypeAndChannel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuerySequence(
            gateway,
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            [
                Row(userId, "PipelineFailure", "Email", false),
            ]),
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            [
                Row(userId, "PipelineFailure", "Email", true),
            ]));
        var capturedCommandTypes = new List<string>();
        SetupWriteCapturingCommandType(gateway, GenericResult<int>.Success(1), capturedCommandTypes);
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value[0].IsEnabled.ShouldBeTrue();
        capturedCommandTypes.ShouldHaveSingleItem();
        capturedCommandTypes[0].ShouldBe("Update");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesMatchesExistingRowsCaseInsensitivelyOnNotificationTypeAndChannel()
    {
        // Arrange — existing row is lowercase; the incoming preference uses different casing.
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuerySequence(
            gateway,
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            [
                Row(userId, "pipelinefailure", "email", false),
            ]),
            GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            [
                Row(userId, "pipelinefailure", "email", true),
            ]));
        var capturedCommandTypes = new List<string>();
        SetupWriteCapturingCommandType(gateway, GenericResult<int>.Success(1), capturedCommandTypes);
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert — case-insensitive natural-key match routes to Update, not Insert.
        capturedCommandTypes.ShouldHaveSingleItem();
        capturedCommandTypes[0].ShouldBe("Update");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesReturnsFailureWhenTheUpdateWriteFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
        [
            Row(userId, "PipelineFailure", "Email", false),
        ]));
        SetupWrite(gateway, GenericResult<int>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>(m => m.Message == "write failed")));
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to persist notification preference");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesReturnsFailureWhenTheInsertWriteFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            Array.Empty<UserNotificationPreferenceConfiguration>()));
        SetupWrite(gateway, GenericResult<int>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>(m => m.Message == "write failed")));
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to persist notification preference");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesReturnsFailureWhenAnExceptionIsThrownDuringSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        SetupQuery(gateway, GenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>.Success(
            Array.Empty<UserNotificationPreferenceConfiguration>()));
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("write boom"));
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act
        var result = await sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Failed to query notification preferences");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task SavePreferencesRethrowsOperationCanceledExceptionWithoutWrappingIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = CreateService(gateway);
        var preferences = new List<NotificationPreference>
        {
            new() { NotificationType = "PipelineFailure", Channel = "Email", IsEnabled = true },
        };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.SavePreferences(userId, preferences, TestContext.Current.CancellationToken));
    }
}
