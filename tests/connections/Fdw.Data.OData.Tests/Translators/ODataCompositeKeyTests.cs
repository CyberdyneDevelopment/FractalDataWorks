using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.OData;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Translators;

/// <summary>
/// An OData resource is addressed by one id in the request path. A container whose primary key has more than one column cannot be
/// addressed that way, and matching it on its first column alone would address every resource that shares that value, so an update or a
/// delete on it is refused with its own code (FDW-6309).
/// </summary>
public sealed class ODataCompositeKeyTests
{
    private static Mock<IStorageContainer> Container(params string[] key)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("OrderLines");
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        container.Setup(c => c.Keys).Returns(TestPrimaryKeys.On(key));
        return container;
    }

    private static Mock<IDataCommand> Command(Dictionary<string, object> metadata)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase));
        return command;
    }

    private static FilterExpression OrderIdIs(int value) => new()
    {
        Root = new FilterCondition { PropertyName = "OrderId", Operator = new EqualOperator(), Value = value },
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateOfACompositeKeyContainerIsRefusedWithItsOwnCode()
    {
        var command = Command(new Dictionary<string, object> { ["Data"] = new { OrderId = 7, LineNo = 2 }, ["Filter"] = OrderIdIs(7) });

        var result = await new ODataUpdateTranslator().Translate(command.Object, Container("OrderId", "LineNo").Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("CompositePrimaryKeyNotSupported");
        result.Details!.GetValue<string>("Container").ShouldBe("OrderLines");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteOfACompositeKeyContainerIsRefusedWithItsOwnCode()
    {
        var command = Command(new Dictionary<string, object> { ["Filter"] = OrderIdIs(7) });

        var result = await new ODataDeleteTranslator().Translate(command.Object, Container("OrderId", "LineNo").Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("CompositePrimaryKeyNotSupported");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteOfASingleColumnKeyContainerAddressesTheResourceByTheKeyCondition()
    {
        var command = Command(new Dictionary<string, object> { ["Filter"] = OrderIdIs(7) });

        var result = await new ODataDeleteTranslator().Translate(command.Object, Container("OrderId").Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/OrderLines/7");
    }
}
