using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Extensions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Connections.FileSystem.Tests;

/// <summary>
/// Proves the FileSystem connection's write path (ConfigurationSave / Update / ConfigurationDelete) is
/// REAL — it reads the container's current rows, mutates the in-memory row list per version-on-write
/// semantics, and rewrites the whole JSON file — instead of silently falling through to a read. Each test
/// drives the UNCHANGED <c>IConfigurationGateway.Execute</c> against a temp-root FileSystem store and then
/// asserts on the raw JSON file the connection actually wrote.
/// </summary>
/// <remarks>
/// In the <c>FileSystemRecordFormats</c> collection so the Json record source/writer TypeOptions are
/// registered (via <see cref="RecordFormatRegistrationFixture"/>) before any Execute reads the format
/// collection — the same guarantee the round-trip tests rely on.
/// </remarks>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemConfigurationWriteTests
{
    private const string SecretManagerFile = "sec/SecretManager.json";
    private const string TypedBodyFile = "sec/EnvironmentVariableSecretManager.json";

    private static DataStoreTarget SecretManagerTarget => new("ConfigurationDb", "sec", "SecretManager");
    private static DataStoreTarget TypedBodyTarget => new("ConfigurationDb", "sec", "EnvironmentVariableSecretManager");

    // 1 — ConfigurationSave on a brand-new logical Id: one new row, RowId assigned, IsCurrent=true, IsDeleted=false.
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConfigurationSaveOnNewLogicalIdAppendsOneCurrentRow()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var record = new SecretManagerConfiguration { Id = id, Name = "Alpha", ServiceOptionType = "EnvironmentVariable" };

        var result = await harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<SecretManagerConfiguration>(record), SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldBe(1);

        var rows = harness.ReadRows(SecretManagerFile);
        rows.Count.ShouldBe(1);
        rows[0].GetProperty("RowId").GetInt64().ShouldBe(1);
        rows[0].GetProperty("Id").GetString().ShouldBe(id.ToString());
        rows[0].GetProperty("Name").GetString().ShouldBe("Alpha");
        rows[0].GetProperty("ServiceOptionType").GetString().ShouldBe("EnvironmentVariable");
        rows[0].GetProperty("IsCurrent").GetBoolean().ShouldBeTrue();
        rows[0].GetProperty("IsDeleted").GetBoolean().ShouldBeFalse();
    }

    // 2 — ConfigurationSave on a logical Id that already has a current row: old row's IsCurrent flips to
    // false; a new row is appended with a new RowId and IsCurrent=true (version-on-write, even though the
    // provider normally routes an existing record through Update).
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConfigurationSaveOnExistingLogicalIdRetiresPriorAndAppendsNewVersion()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
        harness.Seed(SecretManagerFile, SecretManagerRowJson(1, id, "Alpha", "EnvironmentVariable", isCurrent: true, isDeleted: false));

        var record = new SecretManagerConfiguration { Id = id, Name = "AlphaV2", ServiceOptionType = "EnvironmentVariable" };
        var result = await harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<SecretManagerConfiguration>(record), SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());

        var rows = harness.ReadRows(SecretManagerFile);
        rows.Count.ShouldBe(2);

        var prior = rows.Single(r => r.GetProperty("RowId").GetInt64() == 1);
        prior.GetProperty("IsCurrent").GetBoolean().ShouldBeFalse();

        var current = rows.Single(r => r.GetProperty("IsCurrent").GetBoolean());
        current.GetProperty("RowId").GetInt64().ShouldBe(2);
        current.GetProperty("Id").GetString().ShouldBe(id.ToString());
        current.GetProperty("Name").GetString().ShouldBe("AlphaV2");
        current.GetProperty("IsDeleted").GetBoolean().ShouldBeFalse();
    }

    // 3 — Update on an existing row: SAME RowId, fields mutated in place, IsCurrent/IsDeleted untouched,
    // row count unchanged.
    [Fact]
    [Trait("Category", "Write")]
    public async Task UpdateMutatesMatchedRowInPlaceKeepingRowIdAndVersionFlags()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
        harness.Seed(SecretManagerFile, SecretManagerRowJson(1, id, "Alpha", "EnvironmentVariable", isCurrent: true, isDeleted: false));

        var record = new SecretManagerConfiguration { Id = id, Name = "AlphaEdited", ServiceOptionType = "AzureKeyVault" };
        var command = Update.In<SecretManagerConfiguration>("SecretManager")
            .DataStore("ConfigurationDb").Path("sec")
            .Where("Id", id)
            .Value(record)
            .Command;

        var result = await harness.Gateway.Execute<int>(command, SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldBe(1);

        var rows = harness.ReadRows(SecretManagerFile);
        rows.Count.ShouldBe(1);
        rows[0].GetProperty("RowId").GetInt64().ShouldBe(1);
        rows[0].GetProperty("Name").GetString().ShouldBe("AlphaEdited");
        rows[0].GetProperty("ServiceOptionType").GetString().ShouldBe("AzureKeyVault");
        rows[0].GetProperty("IsCurrent").GetBoolean().ShouldBeTrue();
        rows[0].GetProperty("IsDeleted").GetBoolean().ShouldBeFalse();
    }

    // 4 — ConfigurationDelete: the matching current row is set IsCurrent=false, IsDeleted=true in place
    // (row count unchanged, no tombstone row added).
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConfigurationDeleteSoftDeletesCurrentRowInPlace()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
        harness.Seed(SecretManagerFile, SecretManagerRowJson(1, id, "Alpha", "EnvironmentVariable", isCurrent: true, isDeleted: false));

        var result = await harness.Gateway.Execute<int>(
            new ConfigurationDeleteCommand(id), SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldBe(1);

        var rows = harness.ReadRows(SecretManagerFile);
        rows.Count.ShouldBe(1);
        rows[0].GetProperty("RowId").GetInt64().ShouldBe(1);
        rows[0].GetProperty("IsCurrent").GetBoolean().ShouldBeFalse();
        rows[0].GetProperty("IsDeleted").GetBoolean().ShouldBeTrue();
    }

    // 5 — FK resolution on CREATE for a typed body: the new row's FK RowId column resolves to the parent's
    // actual current RowId (read from the parent file), not a value on the POCO.
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConfigurationSaveResolvesForeignKeyToParentCurrentRowId()
    {
        using var harness = new GatewayHarness();
        var parentId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        harness.Seed(SecretManagerFile, SecretManagerRowJson(7, parentId, "Parent", "EnvironmentVariable", isCurrent: true, isDeleted: false));

        var body = new EnvironmentVariableConfiguration
        {
            Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            SecretManagerId = parentId,
            Prefix = "X_",
        };
        var result = await harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<EnvironmentVariableConfiguration>(body), TypedBodyTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());

        var rows = harness.ReadRows(TypedBodyFile);
        rows.Count.ShouldBe(1);
        rows[0].GetProperty("SecretManagerRowId").GetInt64().ShouldBe(7);
        rows[0].GetProperty("SecretManagerId").GetString().ShouldBe(parentId.ToString());
        rows[0].GetProperty("RowId").GetInt64().ShouldBe(1);
        rows[0].GetProperty("IsCurrent").GetBoolean().ShouldBeTrue();
        rows[0].GetProperty("Prefix").GetString().ShouldBe("X_");
    }

    // 6 — FK resolution failure (no matching current parent row): fails loud, does NOT write a broken row.
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConfigurationSaveFailsLoudWhenParentForeignKeyRowIsAbsent()
    {
        using var harness = new GatewayHarness();
        harness.Seed(SecretManagerFile, "[]");

        var body = new EnvironmentVariableConfiguration
        {
            Id = Guid.Parse("cccccccc-0000-0000-0000-000000000002"),
            SecretManagerId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000ff"),
            Prefix = "X_",
        };
        var result = await harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<EnvironmentVariableConfiguration>(body), TypedBodyTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        // Why: fail-loud before any write — the typed-body file must never be created with a dangling FK.
        harness.FileExists(TypedBodyFile).ShouldBeFalse();
    }

    // 7 — An unrecognized command type fails loud (message names the type) and does NOT silently read.
    [Fact]
    [Trait("Category", "Write")]
    public async Task UnrecognizedCommandTypeFailsLoudAndDoesNotRead()
    {
        using var harness = new GatewayHarness();
        harness.Seed(SecretManagerFile, SecretManagerRowJson(1, Guid.NewGuid(), "Alpha", "EnvironmentVariable", isCurrent: true, isDeleted: false));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandId).Returns(Guid.NewGuid());
        command.Setup(c => c.CreatedAt).Returns(DateTime.UtcNow);
        command.Setup(c => c.CommandType).Returns("Frobnicate");
        command.Setup(c => c.Category).Returns("Data");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());

        var result = await harness.Gateway.Execute<int>(command.Object, SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage?.ToString().ShouldContain("Frobnicate");
    }

    // 8 — Two concurrent Save() calls on the SAME file must not race: no lost update, no duplicate RowId.
    // Before the per-file lock: both calls read the same empty/current row snapshot, both compute the same
    // "next" RowId from that snapshot, and whichever write lands last silently clobbers the other — one
    // create vanishes entirely, and BOTH calls still report Success. Task.Run (not a plain async call) is
    // used so the two Save() invocations genuinely execute on separate thread-pool threads.
    [Fact]
    [Trait("Category", "Write")]
    public async Task ConcurrentSavesToSameFileDoNotRaceOrLoseUpdates()
    {
        using var harness = new GatewayHarness();
        var idA = Guid.Parse("dddddddd-0000-0000-0000-00000000000a");
        var idB = Guid.Parse("dddddddd-0000-0000-0000-00000000000b");
        var recordA = new SecretManagerConfiguration { Id = idA, Name = "ConcurrentA", ServiceOptionType = "EnvironmentVariable" };
        var recordB = new SecretManagerConfiguration { Id = idB, Name = "ConcurrentB", ServiceOptionType = "EnvironmentVariable" };

        var taskA = Task.Run(() => harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<SecretManagerConfiguration>(recordA), SecretManagerTarget, TestContext.Current.CancellationToken));
        var taskB = Task.Run(() => harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<SecretManagerConfiguration>(recordB), SecretManagerTarget, TestContext.Current.CancellationToken));

        var results = await Task.WhenAll(taskA, taskB);

        results[0].IsSuccess.ShouldBeTrue(results[0].CurrentMessage?.ToString());
        results[1].IsSuccess.ShouldBeTrue(results[1].CurrentMessage?.ToString());

        var rows = harness.ReadRows(SecretManagerFile);
        rows.Count.ShouldBe(2); // NO LOST UPDATE — both concurrent creates must survive.

        var rowIds = rows.Select(r => r.GetProperty("RowId").GetInt64()).ToList();
        rowIds.Distinct().Count().ShouldBe(2); // NO DUPLICATE ROWID.

        var ids = rows.Select(r => r.GetProperty("Id").GetString()).ToHashSet();
        ids.ShouldContain(idA.ToString());
        ids.ShouldContain(idB.ToString());

        foreach (var row in rows)
        {
            row.GetProperty("IsCurrent").GetBoolean().ShouldBeTrue();
            row.GetProperty("IsDeleted").GetBoolean().ShouldBeFalse();
        }
    }

    // 9 — After a successful write, no orphaned .tmp file remains next to the container file — proves the
    // write-then-atomic-rename path (File.Move onto the target) completed and left no stray temp artifact.
    [Fact]
    [Trait("Category", "Write")]
    public async Task SuccessfulSaveLeavesNoOrphanedTempFile()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
        var record = new SecretManagerConfiguration { Id = id, Name = "NoTemp", ServiceOptionType = "EnvironmentVariable" };

        var result = await harness.Gateway.Execute<int>(
            new ConfigurationSaveCommand<SecretManagerConfiguration>(record), SecretManagerTarget, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());

        var secDir = Path.Combine(harness.Root, "sec");
        Directory.EnumerateFiles(secDir, "*.tmp").ShouldBeEmpty();
        File.Exists(Path.Combine(secDir, "SecretManager.json")).ShouldBeTrue();
    }

    // 10 — A JSON row column NOT in the container's declared schema survives Update/ConfigurationDelete
    // rewrites. Before the fix, the JSON item-writer path projected every row through the container's
    // declared field schema (via DataRecord), silently dropping any column the schema doesn't carry — even
    // though the JSON reader decodes every property dynamically with no such projection.
    [Fact]
    [Trait("Category", "Write")]
    public async Task UndeclaredJsonColumnSurvivesRewriteOnUpdateAndDelete()
    {
        using var harness = new GatewayHarness();
        var id = Guid.Parse("ffffffff-0000-0000-0000-000000000001");
        harness.Seed(SecretManagerFile, $$"""
            [
              {
                "RowId": 1,
                "Id": "{{id}}",
                "Name": "Alpha",
                "ServiceOptionType": "EnvironmentVariable",
                "LegacyNote": "keep-me",
                "IsCurrent": true,
                "IsDeleted": false
              }
            ]
            """);

        var updated = new SecretManagerConfiguration { Id = id, Name = "AlphaUpdated", ServiceOptionType = "EnvironmentVariable" };
        var updateCommand = Update.In<SecretManagerConfiguration>("SecretManager")
            .DataStore("ConfigurationDb").Path("sec")
            .Where("Id", id)
            .Value(updated)
            .Command;
        var updateResult = await harness.Gateway.Execute<int>(updateCommand, SecretManagerTarget, TestContext.Current.CancellationToken);
        updateResult.IsSuccess.ShouldBeTrue(updateResult.CurrentMessage?.ToString());

        var afterUpdate = harness.ReadRows(SecretManagerFile);
        afterUpdate.Count.ShouldBe(1);
        afterUpdate[0].GetProperty("Name").GetString().ShouldBe("AlphaUpdated");
        afterUpdate[0].GetProperty("LegacyNote").GetString().ShouldBe("keep-me");

        var deleteResult = await harness.Gateway.Execute<int>(
            new ConfigurationDeleteCommand(id), SecretManagerTarget, TestContext.Current.CancellationToken);
        deleteResult.IsSuccess.ShouldBeTrue(deleteResult.CurrentMessage?.ToString());

        var afterDelete = harness.ReadRows(SecretManagerFile);
        afterDelete.Count.ShouldBe(1);
        afterDelete[0].GetProperty("IsCurrent").GetBoolean().ShouldBeFalse();
        afterDelete[0].GetProperty("IsDeleted").GetBoolean().ShouldBeTrue();
        afterDelete[0].GetProperty("LegacyNote").GetString().ShouldBe("keep-me");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string SecretManagerRowJson(long rowId, Guid id, string name, string serviceOptionType, bool isCurrent, bool isDeleted)
        => $$"""
             [
               {
                 "RowId": {{rowId}},
                 "Id": "{{id}}",
                 "Name": "{{name}}",
                 "ServiceOptionType": "{{serviceOptionType}}",
                 "IsCurrent": {{(isCurrent ? "true" : "false")}},
                 "IsDeleted": {{(isDeleted ? "true" : "false")}}
               }
             ]
             """;

    /// <summary>
    /// Builds a FileSystem-backed <see cref="IConfigurationGateway"/> over a fresh temp root, cloning the
    /// committed <c>configurationSchema.json</c> and repointing its <c>Root</c> to the temp directory so
    /// writes never touch the committed fixtures.
    /// </summary>
    private sealed class GatewayHarness : IDisposable
    {
        private readonly ServiceProvider _provider;

        public string Root { get; }

        public IConfigurationGateway Gateway { get; }

        public GatewayHarness()
        {
            Root = Directory.CreateTempSubdirectory("fdw-fs-write-").FullName;
            Directory.CreateDirectory(Path.Combine(Root, "sec"));
            var schemaPath = WriteSchemaPointingAt(Root);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddConfigurationGateway<FileSystemConnectionFactory>(schemaPath);
            // Why: DefaultConfigurationProvider consumes Lazy<IConfigurationGateway>; mirror the read fixture
            // even though these tests call the gateway directly, so the same registration shape is exercised.
            services.AddSingleton(sp => new Lazy<IConfigurationGateway>(() => sp.GetRequiredService<IConfigurationGateway>()));

            _provider = services.BuildServiceProvider();
            Gateway = _provider.GetRequiredService<IConfigurationGateway>();
        }

        public void Seed(string relativePath, string json)
        {
            var full = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, json);
        }

        public bool FileExists(string relativePath) => File.Exists(Path.Combine(Root, relativePath));

        public IReadOnlyList<JsonElement> ReadRows(string relativePath)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, relativePath)));
            // Why: Clone() detaches each element from the JsonDocument so callers can read them after the
            // document is disposed by this using block.
            return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
        }

        // Why: the committed schema pins Root to the relative "config-data" seed folder; clone it and point
        // Root at the isolated temp directory so each test writes into its own throwaway store.
        private static string WriteSchemaPointingAt(string root)
        {
            var source = Path.Combine(AppContext.BaseDirectory, "configurationSchema.json");
            var node = JsonNode.Parse(File.ReadAllText(source))!;
            node["ConfigurationSchema"]!["Connections"]!.AsArray()[0]!["Configuration"]!["Root"] = root;
            var target = Path.Combine(root, "configurationSchema.json");
            File.WriteAllText(target, node.ToJsonString());
            return target;
        }

        public void Dispose()
        {
            _provider.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
