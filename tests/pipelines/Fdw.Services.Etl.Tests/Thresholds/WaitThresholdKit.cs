using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Tests.Thresholds;

internal static class WaitThresholdKit
{
    public static WaitThresholdLayer Layer(string owner, string mode) => new(owner, WaitThresholdModes.ByName(mode));
}
