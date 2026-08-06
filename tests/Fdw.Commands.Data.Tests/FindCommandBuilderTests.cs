using System.Linq;
using Fdw.Commands.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

public sealed class FindCommandBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithoutSearchReturnsFailure()
    {
        // Arrange
        var builder = new FindCommandBuilder<object>("Store", "dbo", "Customers");

        // Act
        var result = builder.Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("SearchTerm");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithSearchSucceeds()
    {
        // Arrange
        var builder = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme");

        // Act
        var result = builder.Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Why: Build() returns a DataGatewayCall struct; command properties live on Command, addressing on Target.
        var call = result.Value;
        var command = (FindCommand<object>)call.Command;
        command.SearchTerm.ShouldBe("acme");
        call.Target.Container.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InFieldsSetsFieldNames()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .InFields("Name", "Email")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.FieldNames.ShouldNotBeNull();
        command.FieldNames!.Count.ShouldBe(2);
        command.FieldNames[0].ShouldBe("Name");
        command.FieldNames[1].ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CaseSensitiveDefaultsToFalse()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.CaseSensitive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CaseSensitiveSetsToTrue()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .CaseSensitive()
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.CaseSensitive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxResultsSetsLimit()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .MaxResults(50)
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.MaxResults.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DataStoreNameAndPathNamePassedThrough()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("MyStore", "sales", "Orders")
            .Search("widget")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Why: Addressing lives on DataStoreTarget after the target-typed-gateway refactor.
        var target = result.Value.Target;
        target.DataStore.ShouldBe("MyStore");
        target.Path.ShouldBe("sales");
        target.Container.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullDataStoreNameReturnsFailure()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>(null, "dbo", "Customers")
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("DataStoreName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullPathNameReturnsFailure()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", null, "Customers")
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("PathName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullContainerNameReturnsFailure()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", null)
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("ContainerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullSearchTermReturnsFailure()
    {
        // Arrange
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search(null)
            .Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("SearchTerm");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NullFieldNamesDoesNotFail()
    {
        // Arrange & Act — null field names means "search all fields"
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .InFields(null)
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.FieldNames.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void MaxResultsDefaultsToNullWhenNotSet()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.MaxResults.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void FieldNamesDefaultsToNullWhenNotSet()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers")
            .Search("acme")
            .Build();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var command = (FindCommand<object>)result.Value.Command;
        command.FieldNames.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFailureIncludesErrorCode()
    {
        // Arrange & Act
        var result = new FindCommandBuilder<object>(null, null, null)
            .Build();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        var message = result.Messages.First();
        message.Code.ShouldNotBeNullOrWhiteSpace();
        message.Source.ShouldBe("FindCommandBuilder");
    }
}
