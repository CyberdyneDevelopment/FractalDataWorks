using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Messages;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Reads the choices a row format makes from what states them: a container's metadata, or an export request's format values (FDW-3823).
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "why in the fuck would you make a selection that is a default". A separator, a header, an element name or a flag a
/// format reads with is a choice, and a wrong guess at one silently reads or writes the wrong rows, so each is required where it is stated
/// and nothing stands in for one that is missing. The reader reads each in turn, stops after the first that is missing or unusable, and
/// <see cref="Result{T}"/> returns that failure, naming the owner, the format and the key, in place of the options.
/// </remarks>
public sealed class RowFormatChoices
{
    private readonly IReadOnlyDictionary<string, object> _stated;
    private readonly string _owner;
    private readonly string _format;
    private IGenericMessage? _failure;

    /// <summary>Initializes a reader over what states the choices.</summary>
    /// <param name="stated">The stated values by key, such as a container's metadata.</param>
    /// <param name="owner">What states them, such as the container's name, named in a failure.</param>
    /// <param name="format">The format the choices are for, named in a failure.</param>
    public RowFormatChoices(IReadOnlyDictionary<string, object> stated, string owner, string format)
    {
        _stated = stated ?? throw new ArgumentNullException(nameof(stated));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _format = format ?? throw new ArgumentNullException(nameof(format));
    }

    /// <summary>Reads a required flag: a boolean, or the text <c>true</c> or <c>false</c>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The flag; false after a failure, which <see cref="Result{T}"/> then returns in place of the options.</returns>
    public bool Flag(string key)
    {
        if (_failure is not null || !Stated(key, out var value))
            return false;
        if (value is bool flag)
            return flag;
        if (value is string text && bool.TryParse(text, out var parsed))
            return parsed;

        _failure = RowSourceLog.FormatChoiceNotUsable(NullLogger.Instance, _owner, _format, key, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, "true or false");
        return false;
    }

    /// <summary>Reads required text; whitespace is text (a tab is a separator), an empty value is not.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The text; empty after a failure, which <see cref="Result{T}"/> then returns in place of the options.</returns>
    public string Text(string key)
    {
        if (_failure is not null || !Stated(key, out var value))
            return string.Empty;
        if (value is string text && text.Length > 0)
            return text;

        _failure = RowSourceLog.FormatChoiceNotStated(NullLogger.Instance, _owner, _format, key);
        return string.Empty;
    }

    /// <summary>Reads a required single character.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The character; <c>'\0'</c> after a failure, which <see cref="Result{T}"/> then returns in place of the options.</returns>
    public char Character(string key)
    {
        var text = Text(key);
        if (_failure is not null)
            return '\0';
        if (text.Length == 1)
            return text[0];

        _failure = RowSourceLog.FormatChoiceNotUsable(NullLogger.Instance, _owner, _format, key, text, "one character");
        return '\0';
    }

    /// <summary>Reads text that may be left out, where leaving it out is itself the choice (no record selector: the rows are the root).</summary>
    /// <param name="key">The key.</param>
    /// <returns>The text, or null when it is not stated.</returns>
    public string? OptionalText(string key)
        => _stated.TryGetValue(key, out var value) && value is string text && text.Length > 0 ? text : null;

    /// <summary>Returns the options built from the choices, or the first failure in their place.</summary>
    /// <typeparam name="T">The options' type.</typeparam>
    /// <param name="options">The options built from the choices read.</param>
    /// <returns>The options, or the failure naming the first choice that is missing or unusable.</returns>
    public IGenericResult<T> Result<T>(T options)
        => _failure is null ? GenericResult<T>.Success(options) : GenericResult<T>.Failure(_failure);

    private bool Stated(string key, out object value)
    {
        if (_stated.TryGetValue(key, out var found) && found is not null)
        {
            value = found;
            return true;
        }

        _failure = RowSourceLog.FormatChoiceNotStated(NullLogger.Instance, _owner, _format, key);
        value = string.Empty;
        return false;
    }
}
