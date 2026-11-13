using System;
using System.Linq;
using System.Reflection;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-5442: a code a client can receive from a Graph data set route answers its own text on the wire. The error mapper relays a reason only for a code that
/// overrides <c>FormatClientMessage</c>, so a code that does not shows the caller "The request was invalid" and nothing else (the dev walk found this for DATA-21414).
/// This walks every Graph data set code of the data services, so a code added later cannot deviate unseen: it overrides, or it is named here with the reason it is for the log.
/// </summary>
public sealed class GraphDataSetCodeFamiliesTests
{
    /// <summary>The codes whose text is for the log and not for the caller, each with why. Adding a name here is a decision, never a way to make the test pass.</summary>
    private static readonly string[] LogOnly =
    [
        "GraphDataSetRunFailed",       // the engine's own reason for a run that could not finish; the caller gets the named code
        "GraphDataSetRunnerMissing",   // the host has no runner: a wiring fault of the server, not of the request
        "GraphDataSetLimitNotStated",  // the read limit is server configuration that is missing, not the caller's input
    ];

    private static bool InGraphFamily(IResultCode code) =>
        code.Name.StartsWith("GraphDataSet", StringComparison.Ordinal)
        || code.Name is "DataSetFieldHistoryNotKept" or "DataSetFieldStandInsNotTaken" or "DataSetStandInsNotTaken";

    private static bool Overrides(IResultCode code) =>
        code.GetType().GetMethod(nameof(IResultCode.FormatClientMessage), [typeof(IResultDetails)])!.DeclaringType == code.GetType();

    [Fact]
    public void TheGraphFamilyIsFoundByItsNames()
        => DataServiceResultCodes.All().Count(InGraphFamily).ShouldBeGreaterThan(30);

    [Fact]
    public void EveryGraphDataSetCodeAClientCanReceiveAnswersItsOwnText()
        => DataServiceResultCodes.All()
            .Where(InGraphFamily)
            .Where(code => !LogOnly.Contains(code.Name, StringComparer.Ordinal))
            .Where(code => !Overrides(code))
            .Select(code => $"{code.Name} ({code.Code})")
            .ShouldBeEmpty("each Graph data set code overrides FormatClientMessage (as GraphDataSetDraftNotSoundCode does), or is named in LogOnly with the reason it is for the log");

    [Fact]
    public void ACodeNamedAsLogOnlyReallyExists()
        => LogOnly.Except(DataServiceResultCodes.All().Select(code => code.Name), StringComparer.Ordinal).ShouldBeEmpty();
}
