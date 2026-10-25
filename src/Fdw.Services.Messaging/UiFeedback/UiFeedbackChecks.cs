using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Messages;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>
/// What a UI comment must state before it is kept (FDW-2143). Each check names the field it refused and nothing is cut, trimmed into shape or defaulted:
/// a missing urgency is a refusal, not a "Normal".
/// </summary>
internal static class UiFeedbackChecks
{
    private const int MaxScreenLength = 2000;
    private const int MaxRouteLength = 500;
    private const int MaxThemeLength = 50;

    /// <summary>The states a comment can be in.</summary>
    public static readonly IReadOnlyList<string> States = ["Open", "Resolved"];

    /// <summary>
    /// Checks a request to leave a comment, in the order its fields are read.
    /// </summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="request">What the tester sent.</param>
    /// <returns>The refusal naming the first field that is wrong, or <see langword="null"/> when every field is acceptable.</returns>
    public static IGenericMessage? Problem(ILogger logger, CreateUiFeedbackRequest request)
    {
        return Text(logger, "screen", request.Screen, MaxScreenLength)
            ?? Text(logger, "route", request.Route, MaxRouteLength)
            ?? AreaProblem(logger, request.Area)
            ?? ViewportProblem(logger, request.Viewport)
            ?? ThemeProblem(logger, request.Theme)
            ?? AnchorsProblem(logger, request.AnchorsJson)
            ?? UrgencyProblem(logger, request.Urgency)
            ?? TagsProblem(logger, request.Tags)
            ?? BodyProblem(logger, request.Body);
    }

    /// <summary>
    /// Checks a note, for a new comment or an edit.
    /// </summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="body">The note.</param>
    /// <returns>The refusal, or <see langword="null"/> when the note is acceptable.</returns>
    public static IGenericMessage? BodyProblem(ILogger logger, string? body)
        => Text(logger, "body", body, UiFeedbackLimits.MaxBodyLength);

    /// <summary>
    /// Checks the name of a state a list is narrowed by.
    /// </summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="state">The state asked for.</param>
    /// <param name="canonical">The state as the store spells it.</param>
    /// <returns>The refusal, or <see langword="null"/> when the state is one a comment can be in.</returns>
    public static IGenericMessage? StateProblem(ILogger logger, string state, out string canonical)
    {
        var known = States.FirstOrDefault(candidate => string.Equals(candidate, state, StringComparison.OrdinalIgnoreCase));
        canonical = known is null ? string.Empty : known;
        return known is null
            ? MessagingLog.UiFeedbackFieldInvalid(logger, "state", $"is not a state a comment can be in; send one of {string.Join(", ", States)}")
            : null;
    }

    /// <summary>
    /// Checks the name of an urgency.
    /// </summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="urgency">The urgency named.</param>
    /// <param name="canonical">The urgency as its option spells it.</param>
    /// <returns>The refusal, or <see langword="null"/> when the urgency is declared.</returns>
    public static IGenericMessage? UrgencyProblem(ILogger logger, string? urgency, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(urgency))
            return MessagingLog.UiFeedbackFieldMissing(logger, "urgency");

        var option = UiFeedbackUrgencies.ByName(urgency);
        if (ReferenceEquals(option, UiFeedbackUrgencies.NotFound))
            return MessagingLog.UiFeedbackFieldInvalid(
                logger, "urgency", $"is not an urgency; send one of {string.Join(", ", UiFeedbackUrgencies.All().Select(known => known.Name))}");

        canonical = option.Name;
        return null;
    }

    /// <summary>
    /// Checks the name of a tag.
    /// </summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="tag">The tag named.</param>
    /// <param name="canonical">The tag as its option spells it.</param>
    /// <returns>The refusal, or <see langword="null"/> when the tag is declared.</returns>
    public static IGenericMessage? TagProblem(ILogger logger, string? tag, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(tag))
            return MessagingLog.UiFeedbackFieldMissing(logger, "tag");

        var option = UiFeedbackTags.ByName(tag);
        if (ReferenceEquals(option, UiFeedbackTags.NotFound))
            return MessagingLog.UiFeedbackFieldInvalid(
                logger, "tag", $"'{tag}' is not a tag; send any of {string.Join(", ", UiFeedbackTags.All().Select(known => known.Name))}");

        canonical = option.Name;
        return null;
    }

    private static IGenericMessage? UrgencyProblem(ILogger logger, string? urgency) => UrgencyProblem(logger, urgency, out _);

    private static IGenericMessage? Text(ILogger logger, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return MessagingLog.UiFeedbackFieldMissing(logger, field);

        return value.Length > maxLength
            ? MessagingLog.UiFeedbackFieldInvalid(logger, field, $"is longer than {maxLength} characters")
            : null;
    }

    private static IGenericMessage? AreaProblem(ILogger logger, UiFeedbackArea? area)
    {
        if (area is null)
            return MessagingLog.UiFeedbackFieldMissing(logger, "area");

        if (!double.IsFinite(area.X) || !double.IsFinite(area.Y) || !double.IsFinite(area.W) || !double.IsFinite(area.H))
            return MessagingLog.UiFeedbackFieldInvalid(logger, "area", "must state x, y, w and h as numbers");

        return area.W < 0 || area.H < 0
            ? MessagingLog.UiFeedbackFieldInvalid(logger, "area", "must not have a negative w or h")
            : null;
    }

    private static IGenericMessage? ViewportProblem(ILogger logger, UiFeedbackSize? viewport)
    {
        if (viewport is null)
            return MessagingLog.UiFeedbackFieldMissing(logger, "viewport");

        return viewport.W <= 0 || viewport.H <= 0
            ? MessagingLog.UiFeedbackFieldInvalid(logger, "viewport", "must state a w and an h of more than zero")
            : null;
    }

    private static IGenericMessage? ThemeProblem(ILogger logger, string? theme)
    {
        if (theme is null)
            return null;

        if (string.IsNullOrWhiteSpace(theme))
            return MessagingLog.UiFeedbackFieldInvalid(logger, "theme", "is blank; leave it out when there is none");

        return theme.Length > MaxThemeLength
            ? MessagingLog.UiFeedbackFieldInvalid(logger, "theme", $"is longer than {MaxThemeLength} characters")
            : null;
    }

    // A comment names at least one component, each a JSON object, within the limits; the entries are kept as they are and never looked inside (FDW-2343).
    private static IGenericMessage? AnchorsProblem(ILogger logger, IReadOnlyList<string>? anchors)
    {
        if (anchors is null)
            return MessagingLog.UiFeedbackFieldMissing(logger, "anchors");

        if (anchors.Count == 0)
            return MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", "must name at least one component");

        if (anchors.Count > UiFeedbackLimits.MaxAnchors)
            return MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", $"may name at most {UiFeedbackLimits.MaxAnchors} components");

        for (var i = 0; i < anchors.Count; i++)
        {
            var problem = AnchorProblem(logger, anchors[i], i);
            if (problem is not null)
                return problem;
        }

        return null;
    }

    private static IGenericMessage? AnchorProblem(ILogger logger, string? anchorJson, int position)
    {
        if (string.IsNullOrWhiteSpace(anchorJson))
            return MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", $"entry {position + 1} is empty");

        if (anchorJson.Length > UiFeedbackLimits.MaxAnchorLength)
            return MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", $"entry {position + 1} is longer than {UiFeedbackLimits.MaxAnchorLength} characters");

        try
        {
            using var parsed = JsonDocument.Parse(anchorJson);
            return parsed.RootElement.ValueKind == JsonValueKind.Object
                ? null
                : MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", $"entry {position + 1} must be a JSON object");
        }
        catch (JsonException)
        {
            return MessagingLog.UiFeedbackFieldInvalid(logger, "anchors", $"entry {position + 1} is not valid JSON");
        }
    }

    private static IGenericMessage? TagsProblem(ILogger logger, IReadOnlyList<string>? tags)
    {
        if (tags is null)
            return MessagingLog.UiFeedbackFieldInvalid(logger, "tags", "must be a list; send an empty one for none");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            if (TagProblem(logger, tag, out var canonical) is { } problem)
                return problem;

            if (!seen.Add(canonical))
                return MessagingLog.UiFeedbackFieldInvalid(logger, "tags", $"names '{canonical}' more than once");
        }

        return null;
    }
}
