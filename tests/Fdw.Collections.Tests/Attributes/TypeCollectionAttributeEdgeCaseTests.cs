using Fdw.Collections.Attributes;

namespace Fdw.Collections.Tests.Attributes;

public class TypeCollectionAttributeEdgeCaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BaseTypeName_WithTypeWithoutFullName_ReturnsName()
    {
        // Create a generic type parameter which doesn't have a FullName
        var genericType = typeof(List<>).GetGenericArguments()[0];
        var attribute = new TypeCollectionAttribute(
            genericType,
            typeof(object),
            typeof(TypeCollectionAttributeEdgeCaseTests));

        // For generic type parameters, FullName is null, so it should fallback to Name
        attribute.BaseTypeName.ShouldBe(genericType.Name);
    }
}
