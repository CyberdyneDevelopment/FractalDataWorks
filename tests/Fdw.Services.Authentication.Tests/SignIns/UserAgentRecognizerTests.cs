using Fdw.Services.Authentication.SignIns;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.SignIns;

/// <summary>
/// FDW-2729: the browser and the operating system a User-Agent header names. The strings contain one another, so each case below is one
/// the order of the tokens has to get right.
/// </summary>
public sealed class UserAgentRecognizerTests
{
    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0", "Edge")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36", "Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 OPR/115.0.0.0", "Opera")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Safari/605.1.15", "Safari")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/130.0.6723.90 Mobile/15E148 Safari/604.1", "Chrome")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SAMSUNG SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/26.0 Chrome/122.0.0.0 Mobile Safari/537.36", "Samsung Internet")]
    public void ABrowserIsNamedByTheMostSpecificTokenInTheHeader(string userAgent, string expected)
        => UserAgentRecognizer.BrowserOf(userAgent).ShouldBe(expected);

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36", "Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Safari/605.1.15", "macOS")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1", "iOS")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1", "iOS")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36", "Android")]
    [InlineData("Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36", "ChromeOS")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0", "Linux")]
    public void AnOperatingSystemIsNamedByTheMostSpecificTokenInTheHeader(string userAgent, string expected)
        => UserAgentRecognizer.OperatingSystemOf(userAgent).ShouldBe(expected);

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("curl/8.5.0")]
    [InlineData("python-requests/2.32.3")]
    public void AHeaderThatNamesNothingKnownNamesNothing(string? userAgent)
    {
        UserAgentRecognizer.BrowserOf(userAgent).ShouldBeNull();
        UserAgentRecognizer.OperatingSystemOf(userAgent).ShouldBeNull();
    }
}
