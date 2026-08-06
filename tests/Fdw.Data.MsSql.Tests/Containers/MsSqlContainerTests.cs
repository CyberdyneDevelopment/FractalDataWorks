using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Containers;

// Why (foundational redesign): the old ContainerBase-derived TableContainer/ViewContainer/
// StoredProcedureContainer (built from a pre-materialised IContainerSchema) were deleted. The runtime
// containers are now MsSqlTableContainer/MsSqlViewContainer — unified DataContainer subclasses whose
// Schema is a synchronous projection over their IMsSqlDataField child nodes. These tests assert the
// new contract: container type, physical path, supported operations, and schema projection.
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlContainerTests
{
    private static DatabasePath CreatePath(string schema = "dbo", string name = "TestObject")
        => new("", schema, name);

    private static IDataPath CreateParent(string name = "dbo")
    {
        var parent = new Mock<IDataPath>();
        parent.Setup(p => p.Name).Returns(name);
        return parent.Object;
    }

    private static IReadOnlyList<IMsSqlDataField> CreateFields()
        => [new MsSqlDataField("Id", null, 0, false, (DataTypeOptionBase)MsSqlNativeTypes.ByName("Int"), null, null, null, null)];

    private static IGenericResult<IReadOnlyList<ReferencingKeyBinding>> NoReferencingKeys()
        => GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]);

    private static IReadOnlyDictionary<string, object> NoMetadata()
        => new Dictionary<string, object>(System.StringComparer.Ordinal);

    private static MsSqlTableContainer CreateTable(string name = "Customers")
        => new(name, null, CreateParent(), CreateFields(), [], NoReferencingKeys(),
               CreatePath("dbo", name), FormatTypes.Tabular, NoMetadata());

    private static MsSqlViewContainer CreateView(string name = "ActiveCustomers")
        => new(name, null, CreateParent(), CreateFields(), [], NoReferencingKeys(),
               CreatePath("dbo", name), FormatTypes.Tabular, NoMetadata());

    // MsSqlTableContainer Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerSetsName()
    {
        CreateTable("Customers").Name.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerSetsPath()
    {
        var path = CreatePath();
        var sut = new MsSqlTableContainer("Customers", null, CreateParent(), CreateFields(), [],
            NoReferencingKeys(), path, FormatTypes.Tabular, NoMetadata());

        sut.Path.ShouldBe(path);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerProjectsSchemaFromFields()
    {
        var sut = CreateTable();

        sut.Schema.ShouldNotBeNull();
        sut.Schema.Fields.Count.ShouldBe(1);
        sut.Schema.Fields[0].Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerHasTableContainerType()
    {
        var sut = CreateTable();

        sut.ContainerType.ShouldNotBeNull();
        sut.ContainerType.Name.ShouldBe("Table");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerDefaultsToTabularFormat()
    {
        CreateTable().Format.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TableContainerSupportsQueryInsertUpdateDelete()
    {
        var sut = CreateTable();

        sut.SupportedOperations.ShouldContain("Query");
        sut.SupportedOperations.ShouldContain("Insert");
        sut.SupportedOperations.ShouldContain("Update");
        sut.SupportedOperations.ShouldContain("Delete");
        sut.SupportedOperations.Length.ShouldBe(4);
    }

    // MsSqlViewContainer Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewContainerSetsName()
    {
        CreateView("ActiveCustomers").Name.ShouldBe("ActiveCustomers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewContainerSetsPath()
    {
        var path = CreatePath();
        var sut = new MsSqlViewContainer("ActiveCustomers", null, CreateParent(), CreateFields(), [],
            NoReferencingKeys(), path, FormatTypes.Tabular, NoMetadata());

        sut.Path.ShouldBe(path);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewContainerProjectsSchemaFromFields()
    {
        var sut = CreateView();

        sut.Schema.ShouldNotBeNull();
        sut.Schema.Fields.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewContainerHasViewContainerType()
    {
        var sut = CreateView();

        sut.ContainerType.ShouldNotBeNull();
        sut.ContainerType.Name.ShouldBe("View");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ViewContainerSupportsOnlyQuery()
    {
        var sut = CreateView();

        sut.SupportedOperations.ShouldContain("Query");
        sut.SupportedOperations.Length.ShouldBe(1);
    }
}
