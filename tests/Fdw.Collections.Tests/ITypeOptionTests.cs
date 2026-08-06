using Fdw.Collections;

namespace Fdw.Collections.Tests;

public class ITypeOptionTests
{
    private class TestTypeOption : ITypeOption
    {
        public object Id => _id;
        private readonly int _id;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;

        public TestTypeOption(int id)
        {
            _id = id;
        }
    }

    private class TestGenericTypeOption : ITypeOption<int, TestGenericTypeOption>
    {
        public int Id { get; init; }
        object ITypeOption.Id => Id;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_CanBeImplemented()
    {
        var option = new TestTypeOption(1) { Name = "Test", Category = "TestCategory" };

        ((int)option.Id).ShouldBe(1);
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

        ((int)nonGeneric.Id).ShouldBe(3);
        nonGeneric.Name.ShouldBe("Test");
        nonGeneric.Category.ShouldBe("Cat");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ITypeOption_ObjectId_ReturnsBoxedValue()
    {
        var typed = new TestGenericTypeOption { Id = 4, Name = "Test", Category = "Cat" };
        ITypeOption nonGeneric = typed;

        nonGeneric.Id.ShouldBeOfType<int>();
        ((int)nonGeneric.Id).ShouldBe(4);
    }
}
