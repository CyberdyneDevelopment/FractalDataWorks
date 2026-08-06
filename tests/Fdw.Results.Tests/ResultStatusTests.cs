using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for the ResultStatuses TypeCollection and the Status property on GenericResult.
/// </summary>
public class ResultStatusTests
{
    #region ResultStatuses ByName Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ByName_Success_ReturnsCorrectStatus()
    {
        // Act
        var status = ResultStatuses.ByName("Success");

        // Assert
        status.Id.ShouldBe(0);
        status.IsSuccess.ShouldBeTrue();
        status.RequiresAttention.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ByName_SuccessWithWarnings_ReturnsCorrectStatus()
    {
        // Act
        var status = ResultStatuses.ByName("SuccessWithWarnings");

        // Assert
        status.Id.ShouldBe(1);
        status.IsSuccess.ShouldBeTrue();
        status.RequiresAttention.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ByName_SuccessAfterRetry_ReturnsCorrectStatus()
    {
        // Act
        var status = ResultStatuses.ByName("SuccessAfterRetry");

        // Assert
        status.Id.ShouldBe(2);
        status.IsSuccess.ShouldBeTrue();
        status.RequiresAttention.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ByName_PartialSuccess_ReturnsCorrectStatus()
    {
        // Act
        var status = ResultStatuses.ByName("PartialSuccess");

        // Assert
        status.Id.ShouldBe(3);
        status.IsSuccess.ShouldBeTrue();
        status.RequiresAttention.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ByName_Failure_ReturnsCorrectStatus()
    {
        // Act
        var status = ResultStatuses.ByName("Failure");

        // Assert
        status.Id.ShouldBe(4);
        status.IsSuccess.ShouldBeFalse();
        status.RequiresAttention.ShouldBeTrue();
    }

    #endregion

    #region ResultStatuses ById Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ResultStatuses_ById_ReturnsMatchingStatus()
    {
        // Act
        var byId0 = ResultStatuses.ById(0);
        var byId1 = ResultStatuses.ById(1);
        var byId2 = ResultStatuses.ById(2);
        var byId3 = ResultStatuses.ById(3);
        var byId4 = ResultStatuses.ById(4);

        // Assert
        byId0.Name.ShouldBe(ResultStatuses.ByName("Success").Name);
        byId1.Name.ShouldBe(ResultStatuses.ByName("SuccessWithWarnings").Name);
        byId2.Name.ShouldBe(ResultStatuses.ByName("SuccessAfterRetry").Name);
        byId3.Name.ShouldBe(ResultStatuses.ByName("PartialSuccess").Name);
        byId4.Name.ShouldBe(ResultStatuses.ByName("Failure").Name);
    }

    #endregion

    #region GenericResult Status Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResult_Success_HasSuccessStatus()
    {
        // Act
        var result = GenericResult.Success();

        // Assert
        result.Status.Name.ShouldBe("Success");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResult_Failure_HasFailureStatus()
    {
        // Act
        var result = GenericResult.Failure(new GenericMessage("error"));

        // Assert
        result.Status.Name.ShouldBe("Failure");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResult_SuccessWithExplicitStatus_SetsCorrectStatus()
    {
        // Arrange
        var warningStatus = ResultStatuses.ByName("SuccessWithWarnings");
        var message = new GenericMessage("Some warnings were generated during processing");

        // Act
        var result = GenericResult.Success(warningStatus, message);

        // Assert
        result.Status.Name.ShouldBe("SuccessWithWarnings");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResultOfT_Success_HasSuccessStatus()
    {
        // Act
        var result = GenericResult<int>.Success(42);

        // Assert
        result.Status.Name.ShouldBe("Success");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResultOfT_Failure_HasFailureStatus()
    {
        // Act
        var result = GenericResult<int>.Failure(new GenericMessage("error"));

        // Assert
        result.Status.Name.ShouldBe("Failure");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericResultOfT_SuccessWithExplicitStatus_SetsCorrectStatus()
    {
        // Arrange
        var partialStatus = ResultStatuses.ByName("PartialSuccess");
        var message = new GenericMessage("Some items failed during batch processing");

        // Act
        var result = GenericResult<int>.Success(42, partialStatus, message);

        // Assert
        result.Status.Name.ShouldBe("PartialSuccess");
    }

    #endregion
}
