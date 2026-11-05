using Fdw.Security.Hashing;
using Shouldly;

namespace Fdw.Security.Hashing.Tests;

public sealed class PersonalAccessTokenGeneratorTests
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private readonly PersonalAccessTokenGenerator _generator = new();

    [Fact]
    public void ATokenIsPrefixEnvironmentUnderscoreAnd32Base62Characters()
    {
        var token = _generator.Generate("fdw_pat_", "dev");

        token.ShouldStartWith("fdw_pat_dev_");
        var suffix = token["fdw_pat_dev_".Length..];
        suffix.Length.ShouldBe(32);
        suffix.ShouldAllBe(c => Base62Chars.Contains(c));
    }

    [Fact]
    public void TwoTokensDiffer()
        => _generator.Generate("p_", "e").ShouldNotBe(_generator.Generate("p_", "e"));

    [Fact]
    public void ThePrefixIsTheFirstTwentyCharacters()
    {
        var token = _generator.Generate("fdw_pat_", "dev");

        _generator.ExtractPrefix(token).ShouldBe(token[..20]);
    }

    [Fact]
    public void AShortTokenIsItsOwnPrefix()
        => _generator.ExtractPrefix("short").ShouldBe("short");
}
