using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Fdw.Hosting.MsSql.Logging;
using Xunit;
using Shouldly;

namespace Fdw.Hosting.MsSql.Tests;

/// <summary>
/// Smoke tests for the <c>Fdw.Hosting.MsSql</c> assembly.
/// </summary>
/// <remarks>
/// Why: the entire compiled surface of this package is <see cref="HostingMsSqlLog"/> — an
/// intentionally empty MessageLogging partial class. The comment on the type documents that
/// EventIds 520-527 were retired with the ControlDb purge and were never replaced; the package's
/// real job (pulling MsSql connection/secret-manager/credential registrations into a consumer's
/// build via transitive ProjectReferences) has no independently-executable logic of its own to
/// unit test — the compiled DLL does not even emit AssemblyRef entries for those references
/// because nothing in this assembly's own IL touches their types. These tests pin the current,
/// documented shape of the one real type in the assembly so an accidental change (e.g. someone
/// reintroducing a stray EventId or removing the coverage-exclusion) is caught.
/// </remarks>
public sealed class HostingMsSqlLogTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HostingMsSqlLogIsPublicStaticPartialClass()
    {
        // Arrange
        var type = typeof(HostingMsSqlLog);

        // Act & Assert
        type.IsPublic.ShouldBeTrue();
        type.IsClass.ShouldBeTrue();
        // Why: C# `static class` compiles to abstract+sealed at the CLR level — this is the
        // reflection-visible signature of the `public static partial class` declaration.
        type.IsAbstract.ShouldBeTrue();
        type.IsSealed.ShouldBeTrue();
        type.Namespace.ShouldBe("Fdw.Hosting.MsSql.Logging");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HostingMsSqlLogIsExcludedFromCodeCoverage()
    {
        // Arrange
        var type = typeof(HostingMsSqlLog);

        // Act
        var attribute = type.GetCustomAttribute<ExcludeFromCodeCoverageAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Justification.ShouldBe("MessageLogging partial class - implementation is source-generated");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HostingMsSqlLogDeclaresNoPublicMembers()
    {
        // Arrange
        var type = typeof(HostingMsSqlLog);

        // Act
        // Why: documents the current, intentional state — EventIds 520-527 were retired with the
        // ControlDb purge (see the class remarks) and no replacement MessageLogging methods have
        // been added. If this ever fails, it means methods were added and this test should be
        // updated to assert their shape, not relaxed away.
        var publicMembers = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.DeclaringType == type)
            .ToList();

        // Assert
        publicMembers.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void AssemblyIdentityIsFdwHostingMsSql()
    {
        // Arrange
        var assembly = typeof(HostingMsSqlLog).Assembly;

        // Act
        var name = assembly.GetName();

        // Assert
        name.Name.ShouldBe("Fdw.Hosting.MsSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ProjectFileDeclaresDocumentedTransitiveDependencies()
    {
        // Arrange
        // Why: the compiled DLL emits no AssemblyRef for Fdw.Hosting / Fdw.Services.Connections.MsSql /
        // Fdw.Services.Credentials.Sql /
        // Fdw.Security.Hashing / Fdw.UI.Components.Blazor.MsSql / Fdw.Web.Analytics.Clients because
        // HostingMsSqlLog never touches their types directly — this package's entire purpose is to
        // pull those ProjectReferences transitively into whichever entry-point app references it
        // (so their [ServiceTypeOption]/[TypeOption] registrations land in the consumer's build
        // output). The only place that composition is recorded is the .csproj itself, so this test
        // reads it directly to guard against someone silently dropping one of the README-documented
        // dependencies.
        var csprojPath = FindCsprojPath();
        var content = System.IO.File.ReadAllText(csprojPath);

        // Act & Assert
        content.ShouldContain("Fdw.Hosting.csproj");
        content.ShouldContain("Fdw.Services.Connections.MsSql.csproj");
        content.ShouldContain("Fdw.Services.Credentials.Sql.csproj");
        content.ShouldContain("Fdw.Security.Hashing.csproj");
        content.ShouldContain("Fdw.UI.Components.Blazor.MsSql.csproj");
        content.ShouldContain("Fdw.Web.Analytics.Clients.csproj");

        // Why NOT SecretManagers.EnvironmentVariable: the concrete secret managers moved to
        // reference-servicetypes, so this package no longer pulls one transitively and an entry-point
        // app picks the secret manager it wants by referencing that package instead. Asserted absent
        // rather than simply deleted, so re-adding the reference here is a deliberate act that trips a
        // test rather than a silent reversal of the migration.
        content.ShouldNotContain("Fdw.Services.SecretManagers.EnvironmentVariable.csproj");
    }

    private static string FindCsprojPath()
    {
        // Why: walk up from the test runner's base directory (bin/Debug/net10.0/...) to the repo's
        // "public" root, then down into the source project — avoids hardcoding a machine-specific
        // absolute path while still exercising the real, on-disk project file.
        var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "Fdw.DeveloperKit.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository root (Fdw.DeveloperKit.slnx) from the test output directory.");
        }

        return System.IO.Path.Combine(directory.FullName, "src", "Fdw.Hosting.MsSql", "Fdw.Hosting.MsSql.csproj");
    }
}
