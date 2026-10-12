using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Notifications.Abstractions.Tests;

/// <summary>
/// Tests for NotificationResult class.
/// </summary>
public class NotificationResultTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void SuccessCreatesSuccessfulResult()
    {
        // Arrange
        var requestId = "test-request-123";
        var deliveryId = "delivery-456";

        // Act
        var result = NotificationResult.Success(requestId, deliveryId);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(NotificationStatuses.Sent);
        result.ErrorMessage.ShouldBeNull();
        result.DeliveryId.ShouldBe(deliveryId);
        result.RetryCount.ShouldBe(0);
        result.SentAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void SuccessWithoutDeliveryIdCreatesSuccessfulResult()
    {
        // Arrange
        var requestId = "test-request-123";

        // Act
        var result = NotificationResult.Success(requestId);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(NotificationStatuses.Sent);
        result.ErrorMessage.ShouldBeNull();
        result.DeliveryId.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void DeliveredCreatesDeliveredResult()
    {
        // Arrange
        var requestId = "test-request-123";
        var deliveryId = "delivery-456";

        // Act
        var result = NotificationResult.Delivered(requestId, deliveryId);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(NotificationStatuses.Delivered);
        result.ErrorMessage.ShouldBeNull();
        result.DeliveryId.ShouldBe(deliveryId);
        result.RetryCount.ShouldBe(0);
        result.SentAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void FailedCreatesFailureResult()
    {
        // Arrange
        var requestId = "test-request-123";
        var errorMessage = "Connection failed";
        var retryCount = 3;

        // Act
        var result = NotificationResult.Failed(requestId, errorMessage, retryCount);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(NotificationStatuses.Failed);
        result.ErrorMessage.ShouldBe(errorMessage);
        result.DeliveryId.ShouldBeNull();
        result.RetryCount.ShouldBe(retryCount);
        result.SentAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void FailedWithoutRetryCountCreatesFailureResult()
    {
        // Arrange
        var requestId = "test-request-123";
        var errorMessage = "Connection failed";

        // Act
        var result = NotificationResult.Failed(requestId, errorMessage);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(NotificationStatuses.Failed);
        result.ErrorMessage.ShouldBe(errorMessage);
        result.DeliveryId.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void RejectedCreatesRejectedResult()
    {
        // Arrange
        var requestId = "test-request-123";
        var reason = "Invalid recipient";

        // Act
        var result = NotificationResult.Rejected(requestId, reason);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(NotificationStatuses.Rejected);
        result.ErrorMessage.ShouldBe(reason);
        result.DeliveryId.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
        result.SentAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void PendingCreatesPendingResult()
    {
        // Arrange
        var requestId = "test-request-123";

        // Act
        var result = NotificationResult.Pending(requestId);

        // Assert
        result.RequestId.ShouldBe(requestId);
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(NotificationStatuses.Pending);
        result.ErrorMessage.ShouldBeNull();
        result.DeliveryId.ShouldBeNull();
        result.RetryCount.ShouldBe(0);
        result.SentAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
