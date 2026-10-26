using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Schema;
using Fdw.Services.Connections;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// Discovery records a field's two types (FDW-1588): the store's own in <c>DataType</c> and the portable type in
/// <c>FrameworkDataType</c>, and a refresh repairs a field discovered before, without replacing a portable type someone stated.
/// </summary>
public sealed class DiscoveredSchemaMergeTypeTests
{
    private const string PathValue = "dbo";
    private const string ContainerName = "Orders";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoveryRecordsTheStoreTypeAndThePortableType()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(store, [Source(DiscoveredField("Name", "nvarchar", "String"))], refresh: false);

        var field = store.Paths[0].Containers[0].Fields[0];
        field.DataType.ShouldBe("nvarchar");
        field.FrameworkDataType.ShouldBe("String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DiscoveryLeavesThePortableTypeUnstatedForAStoreTypeWithoutOne()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(store, [Source(DiscoveredField("Payload", "sql_variant", portableType: null))], refresh: false);

        var field = store.Paths[0].Containers[0].Fields[0];
        field.DataType.ShouldBe("sql_variant");
        field.FrameworkDataType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RefreshRepairsAFieldDiscoveredWithADotNetName()
    {
        var store = StoreWith(new DataContainerFieldConfiguration { Name = "Name", DataType = "String", Ordinal = 1 });

        var result = DiscoveredSchemaMerge.Merge(store, [Source(DiscoveredField("Name", "nvarchar", "String"))], refresh: true);

        var field = store.Paths[0].Containers[0].Fields[0];
        field.DataType.ShouldBe("nvarchar");
        field.FrameworkDataType.ShouldBe("String");
        result.FieldsUpdated.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RefreshNeverReplacesAStatedPortableType()
    {
        var store = StoreWith(new DataContainerFieldConfiguration
        {
            Name = "Shipped", DataType = "datetime2", FrameworkDataType = "Date", Ordinal = 1, IsNullable = true,
        });

        var result = DiscoveredSchemaMerge.Merge(store, [Source(DiscoveredField("Shipped", "datetime2", "DateTime"))], refresh: true);

        store.Paths[0].Containers[0].Fields[0].FrameworkDataType.ShouldBe("Date");
        result.FieldsUpdated.ShouldBe(0);
    }

    private static DataStoreImplementationConfiguration StoreWith(DataContainerFieldConfiguration field)
    {
        var store = new DataStoreImplementationConfiguration();
        store.Paths.Add(new DataPathConfiguration
        {
            Name = PathValue,
            PathValue = PathValue,
            Containers = [new DataContainerConfiguration { Name = ContainerName, Fields = [field] }],
        });
        return store;
    }

    private static Field DiscoveredField(string name, string storeType, string? portableType)
    {
        var role = new Mock<IDatumType>();
        role.SetupGet(r => r.IsKeyRole).Returns(false);
        return new Field
        {
            Name = name,
            FieldType = new SimpleFieldType { TypeName = storeType, ClrType = typeof(object) },
            ExplicitType = portableType is null ? null : PortableType(portableType),
            Role = role.Object,
            IsNullable = true,
        };
    }

    private static IDataType PortableType(string name)
    {
        var type = new Mock<IDataType>();
        type.SetupGet(t => t.Name).Returns(name);
        return type.Object;
    }

    private static IStorageContainer Source(params IField[] fields)
    {
        var containerType = new Mock<IContainerType>();
        containerType.SetupGet(t => t.Name).Returns("Table");
        var format = new Mock<IFormatType>();
        format.SetupGet(f => f.Name).Returns("Tabular");
        var path = new Mock<IPath>();
        path.SetupGet(p => p.PathValue).Returns(PathValue);
        var schema = new Mock<IContainerSchema>();
        schema.SetupGet(s => s.Fields).Returns(new List<IField>(fields));

        var source = new Mock<IStorageContainer>();
        source.SetupGet(s => s.Name).Returns(ContainerName);
        source.SetupGet(s => s.ContainerType).Returns(containerType.Object);
        source.SetupGet(s => s.Format).Returns(format.Object);
        source.SetupGet(s => s.Path).Returns(path.Object);
        source.SetupGet(s => s.Schema).Returns(schema.Object);
        return source.Object;
    }
}
