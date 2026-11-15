using Fdw.Commands.Data;

namespace Fdw.Commands.Data.Tests;

// FDW-5918: each thing a find needs and was not given is refused with a coded message of its own, not a plain string.
public sealed class FindCommandBuilderResultCodeTests
{
    // The meaning of all four is "a required value was not provided": the catalog's canonical RequiredValueMissing, 20000, in this domain's type code.
    private const string RequiredValueMissingCode = "CMDDATA-20000";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ADataStoreNameNotGivenIsRefusedWithItsOwnCodedMessage()
    {
        var result = new FindCommandBuilder<object>(null, "dbo", "Customers").Search("acme").Build();

        result.IsSuccess.ShouldBeFalse();
        var message = result.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RequiredValueMissingCode);
        message.Message.ShouldContain("DataStoreName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void APathNameNotGivenIsRefusedWithItsOwnCodedMessage()
    {
        var result = new FindCommandBuilder<object>("Store", null, "Customers").Search("acme").Build();

        result.IsSuccess.ShouldBeFalse();
        var message = result.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RequiredValueMissingCode);
        message.Message.ShouldContain("PathName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AContainerNameNotGivenIsRefusedWithItsOwnCodedMessage()
    {
        var result = new FindCommandBuilder<object>("Store", "dbo", null).Search("acme").Build();

        result.IsSuccess.ShouldBeFalse();
        var message = result.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RequiredValueMissingCode);
        message.Message.ShouldContain("ContainerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASearchTermNotGivenIsRefusedWithItsOwnCodedMessage()
    {
        var result = new FindCommandBuilder<object>("Store", "dbo", "Customers").Build();

        result.IsSuccess.ShouldBeFalse();
        var message = result.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RequiredValueMissingCode);
        message.Message.ShouldContain("Search");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void WhenEverythingIsMissingTheFirstRefusalIsTheDataStoreNameAsBefore()
    {
        var result = new FindCommandBuilder<object>(null, null, null).Build();

        result.IsSuccess.ShouldBeFalse();
        var message = result.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RequiredValueMissingCode);
        message.Message.ShouldContain("DataStoreName");
    }
}
