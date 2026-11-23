using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// The primary key a container double declares in <see cref="IStorageContainer.Keys"/>, which is where a container states its key
/// (FDW-6309). Columns are listed in key order.
/// </summary>
internal static class TestPrimaryKeys
{
    /// <summary>A primary key over <paramref name="columns"/>, in the order given.</summary>
    /// <param name="columns">The key's columns, in key order; one is a single-column key, more is a composite key.</param>
    /// <returns>The keys a container with that primary key declares.</returns>
    internal static IReadOnlyList<IContainerKey> On(params string[] columns)
    {
        var keyFields = columns.Select((column, ordinal) =>
        {
            var local = new Mock<IDataField>();
            local.Setup(f => f.Name).Returns(column);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(local.Object);
            keyField.Setup(k => k.Ordinal).Returns(ordinal);
            return keyField.Object;
        }).ToList();

        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns("PK");
        key.Setup(k => k.KeyType).Returns(new PrimaryKeyType());
        key.Setup(k => k.KeyFields).Returns(keyFields);
        return [key.Object];
    }
}
