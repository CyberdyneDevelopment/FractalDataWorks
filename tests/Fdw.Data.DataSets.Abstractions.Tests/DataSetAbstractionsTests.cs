using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class DataSetAbstractionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanLoadAssembly()
    {
        var assembly = typeof(IDataSetType).Assembly;
        assembly.ShouldNotBeNull();
        assembly.GetName().Name.ShouldBe("Fdw.Data.DataSets.Abstractions");
    }
}
