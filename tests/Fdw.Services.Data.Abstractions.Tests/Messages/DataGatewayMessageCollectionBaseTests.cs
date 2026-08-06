using Fdw.Messages;
using Fdw.Services.Data.Abstractions.Messages;

namespace Fdw.Services.Data.Abstractions.Tests.Messages;

public class DataGatewayMessageCollectionBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromMessageCollectionBase()
    {
        // Arrange
        var baseType = typeof(DataGatewayMessageCollectionBase);

        // Act
        var isAssignable = typeof(MessageCollectionBase<DataGatewayMessage>).IsAssignableFrom(baseType);

        // Assert
        isAssignable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsAbstractClass()
    {
        // Arrange
        var type = typeof(DataGatewayMessageCollectionBase);

        // Act & Assert
        type.IsAbstract.ShouldBeTrue();
    }
}
