using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-4395: the stores ask for the main data gateway by <see cref="DataGatewayNames.Main"/>. The name is the one the shipped option registers under (and the
/// <c>ServiceType</c> of the <c>dg.DataGateway</c> row), so a change to either that leaves the other behind fails here and not in a host.
/// </summary>
public sealed class DataGatewayNamesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheMainGatewaysNameIsTheNameTheShippedOptionRegistersUnder()
        => new MainDataGatewayImplementation().Name.ShouldBe(DataGatewayNames.Main);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheMainGatewaysNameStaysMain()
        => DataGatewayNames.Main.ShouldBe("Main");
}
