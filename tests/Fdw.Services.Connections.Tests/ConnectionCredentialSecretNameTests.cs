using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Results;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1185: a stored connection credential is named in a way every secret manager accepts. Azure Key Vault allows only
/// letters, digits and hyphens, so a name with a dot (the old <c>Connection.{id}.{field}</c>) answered 400 BadParameter.
/// </summary>
public sealed class ConnectionCredentialSecretNameTests
{
    private static readonly Regex Accepted = new("^[A-Za-z0-9-]{1,127}$", RegexOptions.CultureInvariant);

    // The credential fields the connection types declare: every MsSql authentication method, plus the HTTP Basic pair the
    // connection tests use. A new method that declares a field this does not cover is covered by the refusal below.
    private static IEnumerable<string> DeclaredFields() =>
        MsSqlAuthenticationTypes.All().SelectMany(method => method.CredentialFields)
            .Concat(["UsernameSecretName", "PasswordSecretName"])
            .Distinct(StringComparer.Ordinal);

    [Fact]
    public void ANameMintedForEveryDeclaredFieldUsesOnlyLettersDigitsAndHyphens()
    {
        foreach (var field in DeclaredFields())
        {
            var minted = ConnectionCredentialSecretName.Mint("Warehouse", field);

            minted.IsSuccess.ShouldBeTrue(field);
            Accepted.IsMatch(minted.Value!).ShouldBeTrue($"{field}: {minted.Value}");
            ConnectionCredentialSecretName.IsStorable(minted.Value!).ShouldBeTrue(field);
        }
    }

    [Fact]
    public void ANameIsTheConnectionPrefixAFreshIdAndTheField()
    {
        var first = ConnectionCredentialSecretName.Mint("Warehouse", "PasswordSecretName").Value!;
        var second = ConnectionCredentialSecretName.Mint("Warehouse", "PasswordSecretName").Value!;

        first.ShouldStartWith("Connection-");
        first.ShouldEndWith("-PasswordSecretName");
        first.ShouldNotBe(second);
    }

    [Theory]
    [InlineData("Password.Key")]
    [InlineData("Password_Key")]
    [InlineData("Password Key")]
    [InlineData("PasswordKey/1")]
    public void AFieldWithACharacterASecretNameCannotHoldIsRefusedNamingIt(string field)
    {
        var minted = ConnectionCredentialSecretName.Mint("Warehouse", field);

        minted.IsFailure.ShouldBeTrue();
        minted.Code!.Name.ShouldBe("CredentialFieldNameRefused");
        var message = ConnectionsResultCodes.ByName("CredentialFieldNameRefused")
            .FormatClientMessage(ResultDetails.Create("name", "Warehouse", "field", field).With("limit", "127"));
        message.ShouldNotBeNull();
        message.ShouldContain(field);
    }

    [Fact]
    public void AFieldThatMakesTheNamePassTheLimitIsRefused()
    {
        var minted = ConnectionCredentialSecretName.Mint("Warehouse", new string('a', ConnectionCredentialSecretName.MaxLength));

        minted.IsFailure.ShouldBeTrue();
        minted.Code!.Name.ShouldBe("CredentialFieldNameRefused");
    }

    [Fact]
    public void ANameTheStoreMintedIsRecognisedAsMinted()
    {
        foreach (var field in DeclaredFields())
            ConnectionCredentialSecretName.IsMinted(ConnectionCredentialSecretName.Mint("Warehouse", field).Value!).ShouldBeTrue(field);
    }

    [Theory]
    [InlineData("Connection-0b833d9aebd94376a308e45a9555fe04-SecretKeyName")]
    [InlineData("Connection.0b833d9aebd94376a308e45a9555fe04.SecretKeyName")]
    public void BothShapesTheStoreHasMintedAreRecognised(string name)
    {
        ConnectionCredentialSecretName.IsMinted(name).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("base-power-api-key")]
    [InlineData("Connection")]
    [InlineData("Connection-1-SecretKeyName")]
    [InlineData("Connection-0b833d9aebd94376a308e45a9555fe04-")]
    [InlineData("Connection-0b833d9aebd94376a308e45a9555fe04.SecretKeyName")]
    [InlineData("Connection-0B833D9AEBD94376A308E45A9555FE04-SecretKeyName")]
    [InlineData("connection-0b833d9aebd94376a308e45a9555fe04-SecretKeyName")]
    [InlineData("Connection-0b833d9aebd94376a308e45a9555fe0g-SecretKeyName")]
    [InlineData("Connection-0b833d9aebd94376a308e45a9555fe04")]
    public void AnyOtherNameIsASecretSomeoneElseKeepsAndIsNotMinted(string name)
    {
        ConnectionCredentialSecretName.IsMinted(name).ShouldBeFalse();
    }

    [Fact]
    public void ANameUnderTheOldDottedShapeIsNotStorable()
    {
        ConnectionCredentialSecretName.IsStorable("Connection.a.SecretKeyName").ShouldBeFalse();
        ConnectionCredentialSecretName.IsStorable(string.Empty).ShouldBeFalse();
    }
}
