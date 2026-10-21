using System;
using Fdw.Services.Pipelines.Webhooks;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Webhooks;

/// <summary>A webhook trigger's secret (FDW-1884): made at random, stored only as a hash, and checked in constant time.</summary>
public sealed class WebhookSecretTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASecretIsUrlSafeAndItsHashIsTheSha256InHex()
    {
        var (secret, hash, prefix) = WebhookSecret.Create();

        secret.Length.ShouldBe(43);
        secret.ShouldAllBe(character => char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_');
        hash.Length.ShouldBe(64);
        hash.ShouldBe(hash.ToLowerInvariant());
        hash.ShouldBe(WebhookSecret.Hash(secret));
        secret.ShouldStartWith(prefix);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TwoSecretsDiffer()
    {
        WebhookSecret.Create().Secret.ShouldNotBe(WebhookSecret.Create().Secret);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ThePlainSecretIsNotWhatIsStored()
    {
        var (secret, hash, _) = WebhookSecret.Create();

        hash.ShouldNotContain(secret);
        hash.ShouldNotBe(secret);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheRightSecretMatchesItsHash()
    {
        var (secret, hash, _) = WebhookSecret.Create();

        WebhookSecret.Matches(secret, hash).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-the-secret")]
    public void ASecretThatIsMissingEmptyOrWrongNeverMatches(string? sent)
    {
        var (_, hash, _) = WebhookSecret.Create();

        WebhookSecret.Matches(sent, hash).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASecretOfAnotherTriggerDoesNotMatch()
    {
        var (_, hash, _) = WebhookSecret.Create();
        var (otherSecret, _, _) = WebhookSecret.Create();

        WebhookSecret.Matches(otherSecret, hash).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ANullHashIsRefusedNotTreatedAsNoSecret()
    {
        Should.Throw<ArgumentNullException>(() => WebhookSecret.Matches("x", null!));
    }
}
