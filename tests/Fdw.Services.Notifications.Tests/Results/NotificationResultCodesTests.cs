using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Notifications.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Notifications.Tests.Results;

/// <summary>
/// Tests for NotificationResultCodes TypeCollection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NotificationResultCodesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsAllResultCodes()
    {
        // Act
        var all = NotificationResultCodes.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsCorrectResultCode()
    {
        // Arrange — catalog numbers are renumber-prone, so resolve the Id from the name.
        var expected = NotificationResultCodes.ByName("NoRecipients");

        // Act
        var result = NotificationResultCodes.ById(expected.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expected.Id);
        result.Name.ShouldBe("NoRecipients");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsCorrectResultCode()
    {
        // Arrange
        const string expectedName = "EmptyMessage";

        // Act
        var result = NotificationResultCodes.ByName(expectedName);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(expectedName);
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        result.Code.ShouldBe($"NOTIFICATION-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByNameWithInvalidNameReturnsNotFound()
    {
        // Act
        var result = NotificationResultCodes.ByName("InvalidName");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
        result.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ByIdWithInvalidIdReturnsNotFound()
    {
        // Act
        var result = NotificationResultCodes.ById(999999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
        result.Id.ShouldBe(0);
        result.Code.ShouldBe("UNKNOWN");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void NoRecipientsCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("NoRecipients");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(21000);
        code.Name.ShouldBe("NoRecipients");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("At least one recipient is required");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EmptyMessageCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("EmptyMessage");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(20000);
        code.Name.ShouldBe("EmptyMessage");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("Message cannot be empty");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void InvalidEmailAddressCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("InvalidEmailAddress");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(20001);
        code.Name.ShouldBe("InvalidEmailAddress");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("Invalid email address: {EmailAddress}");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void InvalidWebhookUrlCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("InvalidWebhookUrl");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(21002);
        code.Name.ShouldBe("InvalidWebhookUrl");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("Invalid webhook URL: {WebhookUrl}");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void NoWebhookUrlCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("NoWebhookUrl");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(21001);
        code.Name.ShouldBe("NoWebhookUrl");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("At least one webhook URL is required, or configure a default webhook URL");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void UseSendMethodCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("UseSendMethod");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(91000);
        code.Name.ShouldBe("UseSendMethod");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("Use Send() method for notification requests");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void UnsupportedCommandCodeHasCorrectProperties()
    {
        // Act
        var code = NotificationResultCodes.ByName("UnsupportedCommand");

        // Assert — meaningful behavioral fields the source still satisfies.
        code.Id.ShouldBe(90004);
        code.Name.ShouldBe("UnsupportedCommand");
        code.Severity.Name.ShouldBe("Error");
        code.MessageTemplate.ShouldBe("Command type {CommandType} is not supported");
        code.IsRetryable.ShouldBeFalse();
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"NOTIFICATION-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("NOTIFICATION");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesHaveUniqueIds()
    {
        // Act
        var all = NotificationResultCodes.All();
        var ids = all.Select(rc => rc.Id).ToList();

        // Assert
        ids.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesHaveUniqueNames()
    {
        // Act
        var all = NotificationResultCodes.All();
        var names = all.Select(rc => rc.Name).ToList();

        // Assert
        names.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesHaveUniqueCodes()
    {
        // Act
        var all = NotificationResultCodes.All();
        var codes = all.Select(rc => rc.Code).ToList();

        // Assert
        codes.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesHaveUniqueEventIds()
    {
        // Act
        var all = NotificationResultCodes.All();
        var eventIds = all.Select(rc => rc.EventId).ToList();

        // Assert
        eventIds.ShouldBeUnique();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "NOTIFICATION-{number}",
        // Id == EventId == number, Domain == "NOTIFICATION". Assert the invariants rather than
        // hardcoding a (renumber-prone) EventId range.
        foreach (var resultCode in NotificationResultCodes.All())
        {
            if (string.Equals(resultCode.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            resultCode.Code.ShouldBe($"NOTIFICATION-{resultCode.Id}");
            resultCode.EventId.ShouldBe(resultCode.Id);
            resultCode.Domain.ShouldBe("NOTIFICATION");
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AllResultCodesHaveNotificationDomain()
    {
        // Act
        var all = NotificationResultCodes.All();

        // Assert — catalog Domain is the uppercase prefix token.
        foreach (var resultCode in all)
        {
            if (string.Equals(resultCode.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            resultCode.Domain.ShouldBe("NOTIFICATION");
        }
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ResultCodeCanBeUsedWithGenericResult()
    {
        // Arrange
        var code = NotificationResultCodes.ByName("NoRecipients");

        // Act
        var result = GenericResult.Failure(code);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ResultCodeWithDetailsCanBeUsedWithGenericResult()
    {
        // Arrange
        var code = NotificationResultCodes.ByName("InvalidEmailAddress");
        var details = ResultDetails.Create().With("EmailAddress", "invalid-email");

        // Act
        var result = GenericResult.Failure(code, details);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
    }
}
