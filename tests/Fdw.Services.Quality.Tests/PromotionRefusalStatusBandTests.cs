using System.Reflection;
using Fdw.MessageLogging;
using Fdw.Services.Quality.Logging;
using Shouldly;
using Xunit;

namespace Fdw.Services.Quality.Tests;

/// <summary>
/// FDW-2886: the promotion endpoints answer a failed result through ResultHttpStatusMapper, which takes the status from the
/// leading digit of the refusal's event id. These tests keep each refusal the endpoints rely on in the band that sets the
/// status a caller must see: not found 404, not approved 409, same environment 400.
/// </summary>
public class PromotionRefusalStatusBandTests
{
    [Theory]
    [InlineData(nameof(PromotionLog.RequestNotFound), 3)]
    [InlineData(nameof(PromotionLog.EnvironmentNotFound), 3)]
    [InlineData(nameof(PromotionLog.NotApproved), 4)]
    [InlineData(nameof(PromotionLog.SameEnvironmentError), 2)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ARefusalIsLoggedInTheBandOfTheStatusItMustAnswer(string method, int band)
    {
        var refusal = typeof(PromotionLog).GetMethod(method, BindingFlags.Public | BindingFlags.Static);

        refusal.ShouldNotBeNull();
        var attribute = refusal.GetCustomAttribute<MessageLoggingAttribute>();
        attribute.ShouldNotBeNull();
        (attribute.EventId / 10000).ShouldBe(band);
    }
}
