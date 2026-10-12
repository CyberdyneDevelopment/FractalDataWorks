using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Tests.RowQuery;

/// <summary>
/// Builds a minimal mocked <see cref="IDataContainer"/> for <see cref="Fdw.Services.Connections.RowQuery.RecordRowValidator"/>
/// and <see cref="Fdw.Services.Connections.RowQuery.RecordColumnValidator"/> tests: only <see cref="IDataContainer.Name"/>
/// and <see cref="IDataContainer.Nodes"/> (the declared field children) are set up — the two members those
/// validators actually read.
/// </summary>
internal static class ContainerStub
{
    /// <summary>
    /// Builds a container whose declared fields are exactly <paramref name="fields"/>.
    /// </summary>
    /// <param name="name">The container name.</param>
    /// <param name="fields">Each declared field's name and <see cref="IDataField.IsNullable"/> declaration.</param>
    public static IDataContainer Build(string name, params (string Name, bool IsNullable)[] fields)
    {
        var fieldNodes = fields.Select(f => BuildField(f.Name, f.IsNullable)).ToList();

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Nodes).Returns(fieldNodes.Cast<IDataNode>().ToList());
        return container.Object;
    }

    private static IDataField BuildField(string name, bool isNullable)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsNullable).Returns(isNullable);
        return field.Object;
    }
}
