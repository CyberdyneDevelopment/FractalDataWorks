using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Sql.Workspace;
using Microsoft.SqlServer.Dac.Model;
using Shouldly;
using Xunit;

namespace Fdw.Sql.Workspace.Tests;

/// <summary>End-to-end tests for <see cref="SqlWorkspace"/> against a sample .sqlproj fixture.</summary>
public sealed class SqlWorkspaceTests
{
    private static string SqlProjPath =>
        Path.Combine(Path.GetDirectoryName(typeof(SqlWorkspaceTests).Assembly.Location)!,
                     "Fixtures", "SampleSqlProject", "SampleSqlProject.sqlproj");

    [Fact]
    public async Task Load_returns_workspace_with_expected_scripts()
    {
        var result = await SqlWorkspace.Load(SqlProjPath, cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var ws = result.Value!;
        ws.ProjectPath.ShouldBe(Path.GetFullPath(SqlProjPath));
        ws.ScriptPaths.Count.ShouldBeGreaterThanOrEqualTo(5);
        ws.ScriptPaths.ShouldContain(p => p.EndsWith("dbo.Customer.sql"));
        ws.ScriptPaths.ShouldContain(p => p.EndsWith("dbo.Orders.sql"));
        ws.ScriptPaths.ShouldContain(p => p.EndsWith("dbo.CustomerOrders.sql"));
    }

    [Fact]
    public async Task Load_missing_file_returns_failure()
    {
        var result = await SqlWorkspace.Load(Path.Combine(Path.GetTempPath(), "does-not-exist.sqlproj"), cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("not found");
    }

    [Fact]
    public async Task TSqlModel_contains_table_and_view_objects()
    {
        var ws = (await SqlWorkspace.Load(SqlProjPath, cancellationToken: TestContext.Current.CancellationToken)).Value!;

        var tables = ws.Model.GetObjects(DacQueryScopes.Default, ModelSchema.Table).ToList();
        tables.ShouldContain(t => t.Name.Parts.Last() == "Customer");
        tables.ShouldContain(t => t.Name.Parts.Last() == "Orders");

        var views = ws.Model.GetObjects(DacQueryScopes.Default, ModelSchema.View).ToList();
        views.ShouldContain(v => v.Name.Parts.Last() == "CustomerOrders");
    }

    [Fact]
    public async Task CreateSnapshot_RestoreSnapshot_round_trip()
    {
        var ws = (await SqlWorkspace.Load(SqlProjPath, cancellationToken: TestContext.Current.CancellationToken)).Value!;

        var customerPath = ws.ScriptPaths.First(p => p.EndsWith("dbo.Customer.sql"));
        var originalText = ws.GetScriptText(customerPath);
        originalText.ShouldNotBeNull();

        var snapId = ws.CreateSnapshot("before-edit", "test snapshot");
        snapId.ShouldNotBeNullOrEmpty();

        // Mutate the in-memory script.
        ws.UpdateScript(customerPath, "-- intentionally invalidated for the test");
        ws.GetScriptText(customerPath).ShouldStartWith("-- intentionally invalidated");

        // Restore: text should come back.
        var restored = ws.RestoreSnapshot(snapId);
        restored.IsSuccess.ShouldBeTrue();
        ws.GetScriptText(customerPath).ShouldBe(originalText);
    }

    [Fact]
    public async Task SetBaseline_then_RevertToBaseline_undoes_intermediate_edits()
    {
        var ws = (await SqlWorkspace.Load(SqlProjPath, cancellationToken: TestContext.Current.CancellationToken)).Value!;
        var path = ws.ScriptPaths.First(p => p.EndsWith("dbo.Customer.sql"));
        var baselineText = ws.GetScriptText(path)!;

        ws.SetBaseline();
        ws.UpdateScript(path, baselineText + "\n-- appended");
        ws.GetScriptText(path)!.ShouldContain("-- appended");

        var reverted = ws.RevertToBaseline();
        reverted.ShouldBeGreaterThan(0);
        ws.GetScriptText(path).ShouldBe(baselineText);
    }

    [Fact]
    public async Task ApplyChanges_writes_modified_scripts_to_disk()
    {
        // Copy the fixture into a fresh temp directory so we can write to it
        // without disturbing the test-output fixture.
        var tempRoot = Path.Combine(Path.GetTempPath(), "SqlWorkspaceTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempRoot);
        try
        {
            CopyDirectory(Path.GetDirectoryName(SqlProjPath)!, tempRoot);
            var tempSqlproj = Path.Combine(tempRoot, "SampleSqlProject.sqlproj");

            var ws = (await SqlWorkspace.Load(tempSqlproj, cancellationToken: TestContext.Current.CancellationToken)).Value!;
            var path = ws.ScriptPaths.First(p => p.EndsWith("dbo.Customer.sql"));
            var marker = "-- ApplyChanges test " + System.Guid.NewGuid().ToString("N");
            ws.UpdateScript(path, (ws.GetScriptText(path) ?? string.Empty) + "\n" + marker);

            var apply = await ws.ApplyChanges(TestContext.Current.CancellationToken);
            apply.IsSuccess.ShouldBeTrue(apply.CurrentMessage);
            apply.Value!.ShouldContain(path);

            var onDisk = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            onDisk.ShouldContain(marker);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(src, dst));
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(src, dst), overwrite: true);
    }
}
