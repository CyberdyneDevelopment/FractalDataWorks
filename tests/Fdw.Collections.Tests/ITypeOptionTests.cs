using Fdw.Collections;

namespace Fdw.Collections.Tests;

public class ITypeOptionTests
{
    private class TestTypeOption : ITypeOption
    {
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    private class TestGenericTypeOption : ITypeOption<int, TestGenericTypeOption>
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_CanBeImplemented()
    {
        var option = new TestTypeOption { Name = "Test", Category = "TestCategory" };

        option.Name.ShouldBe("Test");
        option.Category.ShouldBe("TestCategory");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_Generic_CanBeImplemented()
    {
        var option = new TestGenericTypeOption { Id = 2, Name = "Generic", Category = "Category" };

        option.Id.ShouldBe(2);
        option.Name.ShouldBe("Generic");
        option.Category.ShouldBe("Category");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_Generic_InheritsFromNonGeneric()
    {
        var typed = new TestGenericTypeOption { Id = 3, Name = "Test", Category = "Cat" };
        ITypeOption nonGeneric = typed;

        nonGeneric.Name.ShouldBe("Test");
        nonGeneric.Category.ShouldBe("Cat");
    }
}
