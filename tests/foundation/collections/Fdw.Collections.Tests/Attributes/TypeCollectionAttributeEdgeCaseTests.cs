using Fdw.Collections.Attributes;

namespace Fdw.Collections.Tests.Attributes;

public class TypeCollectionAttributeEdgeCaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BaseTypeName_WithTypeWithoutFullName_IsRefused()
    {
        // Create a generic type parameter which doesn't have a FullName
        var genericType = typeof(List<>).GetGenericArguments()[0];
        var attribute = new TypeCollectionAttribute(
            genericType,
            typeof(object),
            typeof(TypeCollectionAttributeEdgeCaseTests));

        // For generic type parameters, FullName is null; the attribute refuses rather than substituting the short name (FDW-3580).
        Should.Throw<InvalidOperationException>(() => attribute.BaseTypeName);
    }
}
