using Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions;

namespace Fdw.Services.Quality.Abstractions.Tests;

public class PromotionStatusTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllStatusTypes()
    {
        // Act
        var all = PromotionStatusTypes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectStatusType()
    {
        // Arrange
        var all = PromotionStatusTypes.All();
        var first = all.First();

        // Act
        var result = PromotionStatusTypes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
        result.Name.ShouldBe(first.Name);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = PromotionStatusTypes.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsCorrectStatusType()
    {
        // Act
        var result = PromotionStatusTypes.ByName("Pending");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pending");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = PromotionStatusTypes.ByName("UnknownStatus");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        // Act
        var result1 = PromotionStatusTypes.ByName("Pending");
        var result2 = PromotionStatusTypes.ByName("pending");
        var result3 = PromotionStatusTypes.ByName("PENDING");

        // Assert
        result1.Name.ShouldBe("Pending");
        result2.Name.ShouldBe("_Empty");
        result3.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = PromotionStatusTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void PendingStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Pending");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(1);
        status.Name.ShouldBe("Pending");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InProgressStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("InProgress");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(4);
        status.Name.ShouldBe("InProgress");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CompletedStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Completed");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(5);
        status.Name.ShouldBe("Completed");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeTrue();
        status.AllowsExecution.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void FailedStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Failed");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(6);
        status.Name.ShouldBe("Failed");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ApprovedStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Approved");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(2);
        status.Name.ShouldBe("Approved");
        status.IsTerminal.ShouldBeFalse();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RejectedStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Rejected");

        // Assert
        status.ShouldNotBeNull();
        status.Id.ShouldBe(3);
        status.Name.ShouldBe("Rejected");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllStatusTypesHaveUniqueIds()
    {
        // Act
        var all = PromotionStatusTypes.All();
        var ids = all.Select(s => s.Id).ToList();

        // Assert
        ids.Count.ShouldBe(ids.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllStatusTypesHaveUniqueNames()
    {
        // Act
        var all = PromotionStatusTypes.All();
        var names = all.Select(s => s.Name).ToList();

        // Assert
        names.Count.ShouldBe(names.Distinct().Count());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AllStatusTypesImplementInterface()
    {
        // Act
        var all = PromotionStatusTypes.All();

        // Assert
        foreach (var status in all)
        {
            status.ShouldBeAssignableTo<IPromotionStatusType>();
        }
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    [InlineData("Pending", false, false, false)]
    [InlineData("InProgress", false, false, false)]
    [InlineData("Completed", true, true, false)]
    [InlineData("Failed", true, false, false)]
    [InlineData("Approved", false, false, true)]
    [InlineData("Rejected", true, false, false)]
    public void StatusTypeHasExpectedBehaviorFlags(
        string name,
        bool expectedIsTerminal,
        bool expectedIsSuccess,
        bool expectedAllowsExecution)
    {
        // Act
        var status = PromotionStatusTypes.ByName(name);

        // Assert
        status.ShouldNotBeNull();
        status.IsTerminal.ShouldBe(expectedIsTerminal);
        status.IsSuccess.ShouldBe(expectedIsSuccess);
        status.AllowsExecution.ShouldBe(expectedAllowsExecution);
    }
}
