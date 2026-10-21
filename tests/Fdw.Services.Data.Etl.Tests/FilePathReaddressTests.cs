using System.IO;
using Fdw.Data.Files.Paths;

namespace Fdw.Services.Data.Etl.Tests;

/// <summary>FDW-1967: a file path is read at another place by replacing its directory and/or its file name; a part not stated is kept.</summary>
public sealed class FilePathReaddressTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedNameReplacesTheFileAndKeepsTheDirectory()
    {
        var moved = new FilePath(Path.Combine("inbound", "orders.csv")).Readdress(null, "orders-20261019.csv").ShouldBeOfType<FilePath>();

        moved.PathValue.ShouldBe(Path.Combine("inbound", "orders-20261019.csv"));
        moved.FileName.ShouldBe("orders-20261019.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStatedPathReplacesTheDirectoryAndKeepsTheFile()
    {
        var moved = new FilePath(Path.Combine("inbound", "orders.csv")).Readdress("archive", null).ShouldBeOfType<FilePath>();

        moved.PathValue.ShouldBe(Path.Combine("archive", "orders.csv"));
    }
}
