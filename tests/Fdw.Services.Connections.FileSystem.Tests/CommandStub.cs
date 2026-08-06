using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Connections.FileSystem.Tests;

/// <summary>
/// Builds minimal mocked <see cref="IDataCommand"/> values for the FileSystem record round-trip tests:
/// a Query command (read) and an Insert command (write) carrying the rows as its input data.
/// </summary>
internal static class CommandStub
{
    public static IDataCommand Query()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return command.Object;
    }

    public static IDataCommand Insert(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.CommandType).Returns("Insert");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        command.Setup(c => c.InputData).Returns(rows);
        return command.Object;
    }
}
