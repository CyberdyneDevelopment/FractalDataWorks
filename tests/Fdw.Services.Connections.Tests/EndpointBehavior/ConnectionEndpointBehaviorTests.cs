using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

[ExcludeFromCodeCoverage]
public sealed class ConnectionEndpointBehaviorTests
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    // ── Bind ─────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void BindReadsTheTypesOwnFieldsAndKeepsItsDefaults()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));

        var bound = behavior.Bind(Json("""{ "host": "files.example.com", "username": "etl" }"""), Logger);

        bound.IsSuccess.ShouldBeTrue();
        var sftp = bound.Value.ShouldBeOfType<SftpTestConfiguration>();
        sftp.Host.ShouldBe("files.example.com");
        sftp.Username.ShouldBe("etl");
        sftp.Port.ShouldBe(22);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindTreatsNullAsNotSuppliedSoTheDefaultApplies()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));

        var bound = behavior.Bind(Json("""{ "host": "h", "port": null }"""), Logger);

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBeOfType<SftpTestConfiguration>().Port.ShouldBe(22);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindRefusesABodyThatIsNotAnObject()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));

        behavior.Bind(Json("""[ "host" ]"""), Logger).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindRefusesAValueOfTheWrongType()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));

        behavior.Bind(Json("""{ "port": "not a number" }"""), Logger).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindRefusesAConfigurationTypeThatIsNotConcrete()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(IConnectionImplementationConfiguration));

        behavior.Bind(Json("""{ }"""), Logger).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindReadsADeprecatedFieldThroughItsAlias()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        behavior.Alias("server", nameof(SftpTestConfiguration.Host));

        var bound = behavior.Bind(Json("""{ "Server": "legacy.example.com" }"""), Logger);

        bound.Value.ShouldBeOfType<SftpTestConfiguration>().Host.ShouldBe("legacy.example.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindPrefersTheTypedFieldOverItsAlias()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        behavior.Alias("server", nameof(SftpTestConfiguration.Host));

        var bound = behavior.Bind(Json("""{ "server": "old", "host": "new" }"""), Logger);

        bound.Value.ShouldBeOfType<SftpTestConfiguration>().Host.ShouldBe("new");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindRunsTheTypesValidationRules()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        behavior.Validate((configuration, logger) => string.IsNullOrEmpty(((SftpTestConfiguration)configuration).Host)
            ? GenericResult.Failure(Fdw.Services.Connections.Logging.ConnectionEndpointBehaviorLog.ConfigurationRejected(logger, "Sftp", "host is required"))
            : GenericResult.Success());

        behavior.Bind(Json("""{ "username": "etl" }"""), Logger).IsFailure.ShouldBeTrue();
        behavior.Bind(Json("""{ "host": "h" }"""), Logger).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BindReadsMsSqlWithCaseInsensitiveAuthenticationProperties()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(MsSqlConnectionConfiguration));

        var bound = behavior.Bind(
            Json("""{ "server": "db", "database": "Ops", "authenticationType": "SqlAuth", "additionalProperties": { "username": "sa" } }"""),
            Logger);

        var msSql = bound.Value.ShouldBeOfType<MsSqlConnectionConfiguration>();
        msSql.Server.ShouldBe("db");
        msSql.Port.ShouldBe(1433);
        msSql.Encrypt.ShouldBeTrue();
        msSql.TrustServerCertificate.ShouldBeFalse();
        msSql.AdditionalProperties["USERNAME"].ShouldBe("sa");
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void MergeReplacesTheMembersSentAndKeepsTheRest()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(AzureBlobTestConfiguration));
        var stored = new AzureBlobTestConfiguration { Id = Guid.NewGuid(), Name = "blob", AccountName = "acct", ContainerName = "raw" };

        var merged = behavior.Merge(Json("""{ "containerName": "curated" }"""), stored, Logger);

        var blob = merged.Value.ShouldBeOfType<AzureBlobTestConfiguration>();
        blob.ContainerName.ShouldBe("curated");
        blob.AccountName.ShouldBe("acct");
        blob.Id.ShouldBe(stored.Id);
        blob.Name.ShouldBe("blob");
        stored.ContainerName.ShouldBe("raw");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void MergeMergesADictionaryEntryByEntry()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(AzureBlobTestConfiguration));
        var stored = new AzureBlobTestConfiguration();
        stored.Metadata["owner"] = "data-team";
        stored.Metadata["User-Agent"] = "fdw";

        var merged = behavior.Merge(Json("""{ "metadata": { "user-agent": "fdw/2" } }"""), stored, Logger);

        var metadata = merged.Value.ShouldBeOfType<AzureBlobTestConfiguration>().Metadata;
        metadata["owner"].ShouldBe("data-team");
        metadata["User-Agent"].ShouldBe("fdw/2");
        metadata.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void MergeKeepsAStoredSecretWhenTheMaskIsSentBack()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        var stored = new SftpTestConfiguration { Host = "h", SecretKeyName = "sftp-key" };

        var merged = behavior.Merge(
            Json($$"""{ "secretKeyName": "{{ConnectionSecretMask.Value}}", "port": 2222 }"""), stored, Logger);

        var sftp = merged.Value.ShouldBeOfType<SftpTestConfiguration>();
        sftp.SecretKeyName.ShouldBe("sftp-key");
        sftp.Port.ShouldBe(2222);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void MergeKeepsAStoredDictionaryEntryWhenTheMaskIsSentBack()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(MsSqlConnectionConfiguration));
        var stored = new MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Server = "db", AuthenticationType = "SqlAuth" };
        stored.AdditionalProperties["Username"] = "sa";
        stored.AdditionalProperties["SecretKeyName"] = "db-password";

        var merged = behavior.Merge(
            Json($$"""{ "additionalProperties": { "SecretKeyName": "{{ConnectionSecretMask.Value}}", "Username": "etl" } }"""), stored, Logger);

        var msSql = merged.Value.ShouldBeOfType<MsSqlConnectionConfiguration>();
        msSql.AdditionalProperties["SecretKeyName"].ShouldBe("db-password");
        msSql.AdditionalProperties["Username"].ShouldBe("etl");
        msSql.Server.ShouldBe("db");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void MergeReadsADeprecatedFieldThroughItsAlias()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(MsSqlConnectionConfiguration));
        behavior.Alias("authentication", nameof(MsSqlConnectionConfiguration.AdditionalProperties));
        var stored = new MsSqlConnectionConfiguration();
        stored.AdditionalProperties["Username"] = "sa";

        var merged = behavior.Merge(Json("""{ "authentication": { "Domain": "CORP" } }"""), stored, Logger);

        var entries = merged.Value.ShouldBeOfType<MsSqlConnectionConfiguration>().AdditionalProperties;
        entries["Username"].ShouldBe("sa");
        entries["Domain"].ShouldBe("CORP");
    }

    // ── Project ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    public void ProjectShowsOnlyTheTypesOwnSettingsWithSecretsMasked()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        var sftp = new SftpTestConfiguration
        {
            Name = "partner", HealthCheckEnabled = true, Host = "h", Username = "etl", SecretManagerName = "Vault", SecretKeyName = "sftp-key",
        };

        var shown = behavior.Project(sftp, Logger).Value!.AsObject();

        shown["host"]!.GetValue<string>().ShouldBe("h");
        shown["secretKeyName"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
        shown["secretManagerName"]!.GetValue<string>().ShouldBe("Vault");
        shown.ContainsKey("name").ShouldBeFalse();
        shown.ContainsKey("healthCheckEnabled").ShouldBeFalse();
        shown.ContainsKey("id").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ProjectMasksEveryDictionaryEntryUnlessTheTypeSaysOtherwise()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(AzureBlobTestConfiguration));
        var blob = new AzureBlobTestConfiguration { ConnectionStringSecretName = "blob-conn" };
        blob.Metadata["owner"] = "data-team";
        blob.Metadata["empty"] = null;

        var shown = behavior.Project(blob, Logger).Value!.AsObject();

        shown["metadata"]!["owner"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
        shown["metadata"]!["empty"].ShouldBeNull();
        shown["connectionStringSecretName"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ProjectUsesTheTypesOwnMaskForADictionary()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(MsSqlConnectionConfiguration));
        behavior.MaskEntries(nameof(MsSqlConnectionConfiguration.AdditionalProperties), (_, entries) =>
            entries.ToDictionary(
                entry => entry.Key,
                entry => string.Equals(entry.Key, "SecretKeyName", StringComparison.OrdinalIgnoreCase) ? ConnectionSecretMask.Value : entry.Value,
                StringComparer.OrdinalIgnoreCase));
        var msSql = new MsSqlConnectionConfiguration { MaxSerialInsert = 1000, Server = "db", AuthenticationType = "SqlAuth" };
        msSql.AdditionalProperties["Username"] = "sa";
        msSql.AdditionalProperties["SecretKeyName"] = "db-password";

        var shown = behavior.Project(msSql, Logger).Value!.AsObject();

        shown["server"]!.GetValue<string>().ShouldBe("db");
        shown["additionalProperties"]!["Username"]!.GetValue<string>().ShouldBe("sa");
        shown["additionalProperties"]!["SecretKeyName"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ProjectThenMergeRoundTripsWithoutLosingSecrets()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        var stored = new SftpTestConfiguration { Host = "h", Port = 22, SecretKeyName = "sftp-key" };

        var shown = behavior.Project(stored, Logger).Value!;
        var merged = behavior.Merge(Json(shown.ToJsonString()), stored, Logger);

        var sftp = merged.Value.ShouldBeOfType<SftpTestConfiguration>();
        sftp.SecretKeyName.ShouldBe("sftp-key");
        sftp.Host.ShouldBe("h");
    }
}
