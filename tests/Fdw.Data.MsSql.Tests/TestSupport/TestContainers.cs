using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests.TestSupport;

/// <summary>
/// Builds the container doubles the load translator tests run against: a name, a database path, a container type
/// and a list of fields.
/// </summary>
internal static class TestContainers
{
    /// <summary>A field of a test container.</summary>
    /// <param name="Name">The column name.</param>
    /// <param name="IsIdentity">Whether the column is an identity column.</param>
    internal sealed record Column(string Name, bool IsIdentity = false);

    internal static Mock<IDataContainer> Create(string schema, string name, string containerType, params Column[] columns)
    {
        var fields = columns.Select(c =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(c.Name);
            field.Setup(f => f.IsIdentity).Returns(c.IsIdentity);
            field.Setup(f => f.IsComputed).Returns(false);
            return field.Object;
        }).ToArray();

        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields);

        var type = new Mock<IContainerType>();
        type.Setup(t => t.Name).Returns(containerType);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        // IDataContainer redeclares Name with `new`; translators read it through IStorageContainer.
        container.As<IStorageContainer>().Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Path).Returns(new DatabasePath(string.Empty, schema, name));
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ContainerType).Returns(type.Object);
        return container;
    }

    internal static Mock<IDataContainer> Table(string name, params Column[] columns) =>
        Create("dbo", name, "Table", columns);

    internal static Column[] Columns(params string[] names) => names.Select(n => new Column(n)).ToArray();
}
