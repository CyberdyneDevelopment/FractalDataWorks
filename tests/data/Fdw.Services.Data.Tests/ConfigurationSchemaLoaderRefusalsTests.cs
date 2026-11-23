using System;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A schema file the loader cannot use is a coded failure with its own message, never a thrown exception that the phase runner reports with no code of its own
/// (FDW-6433). Each case starts from a file on disk and reads the failure message's own code (not result.Code, FDW-6137).
/// </summary>
[Trait("Category", "SchemaLoader")]
public sealed class ConfigurationSchemaLoaderRefusalsTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger Log = NullLogger.Instance;

    private static IGenericMessage Refusal(string? schemaJson)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"schema-fdw6433-{Guid.NewGuid():N}.json");
        if (schemaJson is not null)
            System.IO.File.WriteAllText(path, schemaJson);

        try
        {
            var loaded = ConfigurationSchemaLoader.Load(path, Log);

            loaded.IsFailure.ShouldBeTrue();
            return loaded.FirstFailureMessage().ShouldNotBeNull().Message;
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFileThatDoesNotExistIsRefusedWithDATA61144NamingThePath()
    {
        var refusal = Refusal(null);

        refusal.Code.ShouldBe("DATA-61144");
        refusal.Message.ShouldContain("schema-fdw6433-");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFileThatIsNotJsonIsRefusedWithDATA61145()
    {
        var refusal = Refusal("{ this is not json");

        refusal.Code.ShouldBe("DATA-61145");
        refusal.Message.ShouldContain("could not be bound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AFileWithNoConfigurationSchemaObjectIsRefusedWithDATA61146()
    {
        var refusal = Refusal("""{"SomethingElse":{}}""");

        refusal.Code.ShouldBe("DATA-61146");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionThatNamesAKindNobodyRegisteredIsRefusedWithDATA61025NamingTheConnectionAndTheKind()
    {
        var refusal = Refusal("""{"ConfigurationSchema":{"Connections":[{"Name":"Platform","Implementation":"NoSuchConnectionKind","Configuration":{}}]}}""");

        refusal.Code.ShouldBe("DATA-61025");
        refusal.Message.ShouldContain("Platform");
        refusal.Message.ShouldContain("NoSuchConnectionKind");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnEntryThatStatesNoNameIsRefusedWithDATA61145ThatSaysWhichEntryIsWrong()
    {
        var refusal = Refusal("""{"ConfigurationSchema":{"SecretManagers":[{"Implementation":"Synthetic","Configuration":{}}]}}""");

        refusal.Code.ShouldBe("DATA-61145");
        refusal.Message.ShouldContain("secret manager entry declares no Name");
    }
}
