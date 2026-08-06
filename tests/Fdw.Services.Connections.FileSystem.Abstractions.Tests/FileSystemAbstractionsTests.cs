using System;
using Fdw.Services.Connections.FileSystem.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.FileSystem.Abstractions.Tests;

public class FileSystemResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_FileNotFound_ReturnsCode()
    {
        var code = FileSystemResultCodes.ByName("FileNotFound");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("FileNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_PathTraversalDenied_ReturnsCode()
    {
        var code = FileSystemResultCodes.ByName("PathTraversalDenied");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("PathTraversalDenied");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_PathOutsideRoot_ReturnsCode()
    {
        var code = FileSystemResultCodes.ByName("PathOutsideRoot");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("PathOutsideRoot");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_RootNotConfigured_ReturnsCode()
    {
        var code = FileSystemResultCodes.ByName("RootNotConfigured");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("RootNotConfigured");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_RootDirectoryDoesNotExist_ReturnsCode()
    {
        var code = FileSystemResultCodes.ByName("RootDirectoryDoesNotExist");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("RootDirectoryDoesNotExist");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_IoFailed_ReturnsCodeAndIsRetryable()
    {
        var code = FileSystemResultCodes.ByName("IoFailed");
        code.ShouldNotBe(FileSystemResultCodes.NotFound);
        code.Name.ShouldBe("IoFailed");
        code.IsRetryable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_ByName_UnknownName_ReturnsNotFound()
    {
        FileSystemResultCodes.ByName("DoesNotExist").ShouldBe(FileSystemResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "FileSystemAbstractions")]
    public void FileSystemResultCodes_AllFollowCatalogInvariants()
    {
        // Codes are categorized catalog numbers: Code == "FS-{number}", Id == EventId == number,
        // Domain == "FS" (the prefix the base ctor passes). Assert the invariants rather than
        // hardcoding the (renumber-prone) per-code numbers.
        foreach (var code in FileSystemResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"FS-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("FS");
        }
    }
}
