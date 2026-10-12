using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class GetSecretManagerVersionsCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new GetSecretManagerVersionsCommand("vault", "mySecret");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("mySecret");
        cmd.CommandType.ShouldBe("GetSecretVersions");
        cmd.IsSecretModifying.ShouldBeFalse();
        cmd.ExpectedResultType.ShouldBe(typeof(IEnumerable<SecretValue>));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateFactoryMethodBuildsCommand()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key");

        cmd.SecretKey.ShouldBe("key");
        cmd.Container.ShouldBe("vault");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateWithIncludeDisabledSetsParameter()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key", includeDisabled: true);

        cmd.Parameters.ShouldContainKey("IncludeDisabled");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateWithMaxResultsSetsParameter()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key", maxResults: 10);

        cmd.Parameters.ShouldContainKey("MaxResults");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersMergesExistingParameters()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key", includeDisabled: true);
        var additional = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(additional);

        updated.Parameters.ShouldContainKey("IncludeDisabled");
        updated.Parameters.ShouldContainKey("custom");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataMergesExistingMetadata()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key");
        var meta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(meta);

        updated.Metadata.ShouldContainKey("source");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithTimeoutSetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(45);
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key");

        var updated = cmd.WithTimeout(timeout);

        updated.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForValidCommand()
    {
        var cmd = GetSecretManagerVersionsCommand.Create("vault", "key");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }
}
