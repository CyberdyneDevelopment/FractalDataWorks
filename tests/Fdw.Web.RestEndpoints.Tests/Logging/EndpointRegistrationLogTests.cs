using Fdw.Messages;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests.Logging;

/// <summary>
/// Tests the narration the endpoint registration chain emits about groups.
/// </summary>
/// <remarks>
/// These lines exist because a host that serves no REST endpoints and a host whose registration
/// chain is broken previously produced the same output — nothing. The one that matters is
/// <c>NoEndpointGroups</c>: it is the line that says a skip was DELIBERATE, and it is emitted in
/// both the registration and the initialization phase because Add and Use are a pair. Pinning the
/// EventId, the level and the formatted text is what keeps that answer readable, since the whole
/// value of the line is what an operator reads in the log when a host serves no endpoints.
///
/// <c>NoEndpointsDeclared</c> is tested alongside them on purpose: it is the surviving FAILURE, and
/// the pair only reads correctly if the deliberate skip stays Debug while the broken chain stays
/// Error.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class EndpointRegistrationLogTests
{
    /// <summary>A host that joined no group says so, naming the phase it is skipping.</summary>
    /// <param name="phase">The phase word the caller passes.</param>
    /// <param name="expected">The line an operator reads.</param>
    /// <remarks>
    /// Both phases are asserted because the phase word is the only thing that distinguishes them in
    /// the log, and it is the evidence that Add and Use skipped together rather than one of them
    /// having been forgotten — the split that produced "No service for type 'FastEndpoint...'".
    /// </remarks>
    [Theory]
    [InlineData(
        "registration",
        "No endpoint groups joined; this host serves no REST endpoints, so FastEndpoints registration is skipped")]
    [InlineData(
        "initialization",
        "No endpoint groups joined; this host serves no REST endpoints, so FastEndpoints initialization is skipped")]
    public void NoEndpointGroupsNamesThePhaseItSkips(string phase, string expected)
    {
        var logger = new RecordingLogger();

        var message = EndpointRegistrationLog.NoEndpointGroups(logger, phase);

        message.Code.ShouldBe("ENDPOINTREG-11023");
        message.Message.ShouldBe(expected);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.Id.ShouldBe(11023);
        entry.EventId.Name.ShouldBe(nameof(EndpointRegistrationLog.NoEndpointGroups));
        entry.Message.ShouldBe(expected);
    }

    /// <summary>Skipping because no group joined is not an error.</summary>
    /// <remarks>
    /// A Blazor skin joins no endpoint group and is not broken. If this severity ever rises, a host
    /// that is behaving correctly starts reporting a fault, which is the judgement the guard exists
    /// to avoid making.
    /// </remarks>
    [Fact]
    public void NoEndpointGroupsIsNotAFailureSeverity()
    {
        EndpointRegistrationLog.NoEndpointGroups(NullLogger.Instance, "registration")
            .ShouldBeOfType<GenericMessage>()
            .Severity.ShouldBe(MessageSeverity.Debug);
    }

    /// <summary>The count of joined groups is stated before any of them registers.</summary>
    [Fact]
    public void EndpointGroupsJoinedStatesTheCount()
    {
        var logger = new RecordingLogger();

        var message = EndpointRegistrationLog.EndpointGroupsJoined(logger, 3);

        message.Code.ShouldBe("ENDPOINTREG-11024");
        message.Message.ShouldBe("Endpoint registration starting over 3 joined group(s)");

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Trace);
        entry.EventId.Id.ShouldBe(11024);
        entry.EventId.Name.ShouldBe(nameof(EndpointRegistrationLog.EndpointGroupsJoined));
        entry.Message.ShouldBe(message.Message);
    }

    /// <summary>Each group is named as it is about to register, with what it holds.</summary>
    [Fact]
    public void EndpointGroupRegisteringNamesTheGroupAndItsMemberCount()
    {
        var logger = new RecordingLogger();

        var message = EndpointRegistrationLog.EndpointGroupRegistering(logger, "ScheduleEndpoints", 7);

        message.Code.ShouldBe("ENDPOINTREG-11025");
        message.Message.ShouldBe("Registering endpoint group ScheduleEndpoints holding 7 option(s)");

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Trace);
        entry.EventId.Id.ShouldBe(11025);
        entry.EventId.Name.ShouldBe(nameof(EndpointRegistrationLog.EndpointGroupRegistering));
        entry.Message.ShouldBe(message.Message);
    }

    /// <summary>What a group actually contributed is reported against the running total.</summary>
    /// <remarks>
    /// This is the line that answers "which group contributed nothing" when the chain ends with
    /// nothing declared, so the contributed count and the running total must both survive into the
    /// text — a line carrying only one of them cannot answer it.
    /// </remarks>
    [Fact]
    public void EndpointGroupContributedReportsTheDeltaAndTheRunningTotal()
    {
        var logger = new RecordingLogger();

        var message = EndpointRegistrationLog.EndpointGroupContributed(logger, "ScheduleEndpoints", 7, 19);

        message.Code.ShouldBe("ENDPOINTREG-11026");
        message.Message.ShouldBe("Endpoint group ScheduleEndpoints contributed 7 endpoint type(s); 19 declared so far");

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.Id.ShouldBe(11026);
        entry.EventId.Name.ShouldBe(nameof(EndpointRegistrationLog.EndpointGroupContributed));
        entry.Message.ShouldBe(message.Message);
    }

    /// <summary>A group that joined and declared nothing is still an error.</summary>
    /// <remarks>
    /// The guard above it changed; this one did not. A host that joined a group has said it serves
    /// REST endpoints, so declaring none is a broken registration chain rather than a skin, and it
    /// must stay Error — the new Debug skip must not have swallowed it.
    /// </remarks>
    [Fact]
    public void NoEndpointsDeclaredIsStillAnError()
    {
        var logger = new RecordingLogger();

        var message = EndpointRegistrationLog.NoEndpointsDeclared(logger);

        message.Code.ShouldBe("ENDPOINTREG-91014");
        message.ShouldBeOfType<GenericMessage>().Severity.ShouldBe(MessageSeverity.Error);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.EventId.Id.ShouldBe(91014);
        entry.EventId.Name.ShouldBe(nameof(EndpointRegistrationLog.NoEndpointsDeclared));
    }

    /// <summary>Every group line carries its own code.</summary>
    /// <remarks>
    /// A duplicated code makes two different conditions indistinguishable to anything filtering on
    /// it, which is the one thing a code is for.
    /// </remarks>
    [Fact]
    public void EachGroupLineCarriesADistinctCode()
    {
        var logger = NullLogger.Instance;

        new[]
        {
            EndpointRegistrationLog.NoEndpointGroups(logger, "registration").Code,
            EndpointRegistrationLog.EndpointGroupsJoined(logger, 0).Code,
            EndpointRegistrationLog.EndpointGroupRegistering(logger, "Any", 0).Code,
            EndpointRegistrationLog.EndpointGroupContributed(logger, "Any", 0, 0).Code,
            EndpointRegistrationLog.NoEndpointsDeclared(logger).Code,
        }.ShouldBeUnique();
    }
}
