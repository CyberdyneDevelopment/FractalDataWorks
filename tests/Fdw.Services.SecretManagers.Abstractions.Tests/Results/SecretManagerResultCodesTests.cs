using System;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Abstractions.Tests.Results;

public class SecretManagerResultCodesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllReturnsAllResultCodes()
    {
        var all = SecretManagerResultCodes.All();

        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(8);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsCorrectResultCode()
    {
        var expected = SecretManagerResultCodes.ByName("NoHandlerFound");
        var result = SecretManagerResultCodes.ById(expected.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(expected.Id);
        result.Name.ShouldBe("NoHandlerFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        var result = SecretManagerResultCodes.ById(99999);

        result.ShouldNotBeNull();
        result.ShouldBe(SecretManagerResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsCorrectResultCode()
    {
        var result = SecretManagerResultCodes.ByName("NoHandlerFound");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("NoHandlerFound");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        result.Code.ShouldBe($"SECRETMANAGER-{result.Id}");
        result.EventId.ShouldBe(result.Id);
        result.Domain.ShouldBe("SECRETMANAGER");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameIsCaseSensitive()
    {
        var found = SecretManagerResultCodes.ByName("NoHandlerFound");
        found.ShouldNotBeNull();
        found.Name.ShouldBe("NoHandlerFound");

        SecretManagerResultCodes.ByName("nohandlerfound").ShouldBe(SecretManagerResultCodes.NotFound);
        SecretManagerResultCodes.ByName("NOHANDLERFOUND").ShouldBe(SecretManagerResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        var result = SecretManagerResultCodes.ByName("UnknownCode");

        result.ShouldNotBeNull();
        result.ShouldBe(SecretManagerResultCodes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NotFoundReturnsEmptyInstance()
    {
        var result = SecretManagerResultCodes.NotFound;

        result.ShouldNotBeNull();
        result.Name.ShouldBe("NotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllResultCodesFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "SECRETMANAGER-{number}",
        // Id == EventId == number, Domain == "SECRETMANAGER". Assert the invariants rather than
        // hardcoding the (renumber-prone) per-code numbers.
        foreach (var result in SecretManagerResultCodes.All())
        {
            if (string.Equals(result.Name, "NotFound", StringComparison.Ordinal))
            {
                continue;
            }

            result.Code.ShouldBe($"SECRETMANAGER-{result.Id}");
            result.EventId.ShouldBe(result.Id);
            result.Domain.ShouldBe("SECRETMANAGER");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoHandlerFoundCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("NoHandlerFound");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("NoHandlerFound");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("No handler found for command type '{CommandType}'");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InvalidCommandTypeCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("InvalidCommandType");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("InvalidCommandType");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Command must be of type {ExpectedType}, but was {ActualType}");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void InvalidExecutionContextCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("InvalidExecutionContext");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("InvalidExecutionContext");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Execution context must be {ExpectedType}");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecretKeyRequiredCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("SecretKeyRequired");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("SecretKeyRequired");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Secret key is required for {Operation} operation");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecretValueRequiredCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("SecretValueRequired");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("SecretValueRequired");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("SecretValue parameter is required for {Operation} operation");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CertificateExportFailedCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("CertificateExportFailed");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("CertificateExportFailed");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Failed to export certificate '{CertificateName}': {ErrorMessage}");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CertificateNameRequiredCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("CertificateNameRequired");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("CertificateNameRequired");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Certificate name is required for {Operation} operation");
        code.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsFailedCodeHasCorrectProperties()
    {
        var code = SecretManagerResultCodes.ByName("ListSecretsFailed");

        code.ShouldNotBeNull();
        code.Name.ShouldBe("ListSecretsFailed");
        code.Code.ShouldBe($"SECRETMANAGER-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("SECRETMANAGER");
        code.MessageTemplate.ShouldBe("Failed to list secrets: {ErrorMessage}");
        code.IsRetryable.ShouldBeFalse();
    }
}
