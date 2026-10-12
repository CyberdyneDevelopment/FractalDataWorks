using Fdw.Services.Users.Results;

namespace Fdw.Services.Users.Tests.Results;

/// <summary>
/// Tests for UserResultCodes TypeCollection.
/// </summary>
public sealed class UserResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UserNotFoundHasCorrectProperties()
    {
        // Act
        var code = UserResultCodes.ByName("UserNotFound");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UserNotFound");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"USER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("USER");
        code.MessageTemplate.ShouldBe("User not found");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UserAlreadyExistsHasCorrectProperties()
    {
        // Act
        var code = UserResultCodes.ByName("UserAlreadyExists");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UserAlreadyExists");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"USER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("USER");
        code.MessageTemplate.ShouldBe("User '{username}' already exists");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void InvalidCredentialsHasCorrectProperties()
    {
        // Act
        var code = UserResultCodes.ByName("InvalidCredentials");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("InvalidCredentials");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"USER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("USER");
        code.MessageTemplate.ShouldBe("Invalid credentials");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UserInactiveHasCorrectProperties()
    {
        // Act
        var code = UserResultCodes.ByName("UserInactive");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("UserInactive");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"USER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("USER");
        code.MessageTemplate.ShouldBe("User account is inactive");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void QueryFailedHasCorrectProperties()
    {
        // Act
        var code = UserResultCodes.ByName("QueryFailed");

        // Assert
        code.ShouldNotBeNull();
        code.Name.ShouldBe("QueryFailed");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"USER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("USER");
        code.MessageTemplate.ShouldBe("Failed to query user data: {ErrorMessage}");
        code.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllCodesHaveUniqueNames()
    {
        // Act
        var all = UserResultCodes.All();
        var names = all.Select(c => c.Name).ToList();

        // Assert
        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllCodesHaveUniqueCodes()
    {
        // Act
        var all = UserResultCodes.All();
        var codes = all.Select(c => c.Code).ToList();

        // Assert
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllCodesHaveUniqueEventIds()
    {
        // Act
        var all = UserResultCodes.All();
        var eventIds = all.Select(c => c.EventId).ToList();

        // Assert
        eventIds.Distinct().Count().ShouldBe(eventIds.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Catalog scheme: each code's number is its whole identity — Id == EventId == number,
        // Code == "USER-{number}", Domain == "USER". Assert the invariants rather than a fixed
        // EventId range (the numbers are categorized and renumber-prone).
        foreach (var code in UserResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"USER-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("USER");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void RetryableIsExactlyTheCodesARetryCouldChange()
    {
        // Retryable is a claim about the world, not a severity: repeating the call can plausibly
        // give a different answer. QueryFailed is a transport/store failure; ExternalIssuersUnreadable
        // means an authority would not state its issuer, so whether the issuer is resolvable is
        // unknown rather than known-bad. Every other code is a settled "no" that a retry cannot move.
        // Listing them by name is deliberate: adding a retryable code should be a decision someone
        // makes here, not a count that drifts.
        // Act
        var retryable = UserResultCodes.All().Where(c => c.IsRetryable).Select(c => c.Name).ToList();

        // Assert
        retryable.ShouldBe(["ExternalIssuersUnreadable", "QueryFailed"], ignoreOrder: true);
    }
}
