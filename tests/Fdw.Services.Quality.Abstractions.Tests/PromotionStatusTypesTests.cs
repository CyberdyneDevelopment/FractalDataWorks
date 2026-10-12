using Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions;

namespace Fdw.Services.Quality.Abstractions.Tests;

public class PromotionStatusTypesTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void PendingStatusTypeHasCorrectProperties()
    {
        // Act
        var status = PromotionStatusTypes.ByName("Pending");

        // Assert
        status.ShouldNotBeNull();
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
        status.Name.ShouldBe("Rejected");
        status.IsTerminal.ShouldBeTrue();
        status.IsSuccess.ShouldBeFalse();
        status.AllowsExecution.ShouldBeFalse();
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
