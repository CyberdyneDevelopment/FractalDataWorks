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
    public void AllReturnsAllUserResultCodes()
    {
        // Act
        var all = UserResultCodes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByIdReturnsCorrectResultCode()
    {
        // Arrange — resolve the catalog Id by name (numbers are renumber-prone catalog values).
        var expected = UserResultCodes.ByName("UserNotFound");

        // Act
        var result = UserResultCodes.ById(expected.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expected.Id);
        result.Name.ShouldBe("UserNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = UserResultCodes.ById(99999);

        // Assert
        result.ShouldBe(UserResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByNameReturnsCorrectResultCode()
    {
        // Act
        var result = UserResultCodes.ByName("UserNotFound");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("UserNotFound");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        result.Code.ShouldBe($"USER-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("USER");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange & Act
        var found = UserResultCodes.ByName("UserNotFound");
        var lowercase = UserResultCodes.ByName("usernotfound");
        var uppercase = UserResultCodes.ByName("USERNOTFOUND");

        // Assert
        found.ShouldNotBeNull();
        found.Name.ShouldBe("UserNotFound");
        lowercase.ShouldBe(UserResultCodes.NotFound);
        uppercase.ShouldBe(UserResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = UserResultCodes.ByName("UnknownCode");

        // Assert
        result.ShouldBe(UserResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = UserResultCodes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
    }

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
    public void AllCodesHaveUniqueIds()
    {
        // Act
        var all = UserResultCodes.All();
        var ids = all.Select(c => c.Id).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
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
    public void AllCodesHaveUserDomain()
    {
        // Act
        var all = UserResultCodes.All();

        // Assert — catalog prefix is the uppercase token "USER".
        foreach (var code in all)
        {
            code.Domain.ShouldBe("USER");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllCodesHaveNonEmptyMessageTemplate()
    {
        // Act
        var all = UserResultCodes.All();

        // Assert
        foreach (var code in all)
        {
            code.MessageTemplate.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllCodesHaveNonEmptyCode()
    {
        // Act
        var all = UserResultCodes.All();

        // Assert
        foreach (var code in all)
        {
            code.Code.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void OnlyQueryFailedIsRetryable()
    {
        // Act
        var all = UserResultCodes.All();
        var retryable = all.Where(c => c.IsRetryable).ToList();

        // Assert
        retryable.Count.ShouldBe(1);
        retryable[0].Name.ShouldBe("QueryFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByIdWithZeroReturnsNotFound()
    {
        // Act
        var result = UserResultCodes.ById(0);

        // Assert
        result.ShouldBe(UserResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByIdWithNegativeValueReturnsNotFound()
    {
        // Act
        var result = UserResultCodes.ById(-1);

        // Assert
        result.ShouldBe(UserResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByNameWithNullThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => UserResultCodes.ByName(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ByNameWithEmptyStringReturnsNotFound()
    {
        // Act
        var result = UserResultCodes.ByName(string.Empty);

        // Assert
        result.ShouldBe(UserResultCodes.NotFound);
    }
}
