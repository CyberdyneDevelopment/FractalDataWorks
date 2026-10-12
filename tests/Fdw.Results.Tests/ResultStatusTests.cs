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

    #endregion

    #region ResultStatuses ById Tests

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
