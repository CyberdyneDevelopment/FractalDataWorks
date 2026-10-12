using System;
using Fdw.Results;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// Reads the text of a condition into an <see cref="IRecordPredicate"/>, left to right. Whatever it cannot read it
/// reports, with the position; it never guesses.
/// </summary>
internal sealed class RecordPredicateScanner
{
    private const string WordBreaks = "()'\"=!<>&|";

    private readonly string _text;
    private int _position;
    private string? _problem;

    private RecordPredicateScanner(string text)
    {
        _text = text;
    }

    public static IGenericResult<IRecordPredicate> Read(string expression, ILogger logger)
    {
        var scanner = new RecordPredicateScanner(expression);
        var predicate = scanner.Or();
        if (predicate is not null)
        {
            scanner.RequireEnd();
        }

        if (scanner._problem is not null)
        {
            return GenericResult<IRecordPredicate>.Failure(
                EtlResultCodes.ByName("RecordPredicateInvalid"),
                logger,
                ResultDetails.Create("Expression", expression, "Reason", scanner._problem));
        }

        return GenericResult<IRecordPredicate>.Success(predicate!);
    }

    private IRecordPredicate? Or()
    {
        var left = And();
        while (left is not null && TakeConnective(RecordPredicateKeywords.Or, "||"))
        {
            var right = And();
            left = right is null ? null : new OrRecordPredicate(left, right);
        }

        return left;
    }

    private IRecordPredicate? And()
    {
        var left = Not();
        while (left is not null && TakeConnective(RecordPredicateKeywords.And, "&&"))
        {
            var right = Not();
            left = right is null ? null : new AndRecordPredicate(left, right);
        }

        return left;
    }

    private IRecordPredicate? Not()
    {
        SkipSpaces();
        if (StartsAt(_position, "!") && !StartsAt(_position, RecordPredicateKeywords.NotEqual))
        {
            _position++;
            return NotOf(Not());
        }

        if (WordAt(_position, RecordPredicateKeywords.Not))
        {
            _position += RecordPredicateKeywords.Not.Length;
            return NotOf(Not());
        }

        return Primary();
    }

    private static NotRecordPredicate? NotOf(IRecordPredicate? inner) => inner is null ? null : new NotRecordPredicate(inner);

    private IRecordPredicate? Primary()
    {
        SkipSpaces();
        if (_position >= _text.Length)
        {
            return Fail("a condition is missing");
        }

        if (_text[_position] != '(')
        {
            return Comparison();
        }

        _position++;
        var inner = Or();
        if (inner is null)
        {
            return null;
        }

        SkipSpaces();
        if (_position >= _text.Length || _text[_position] != ')')
        {
            return Fail("a closing parenthesis is missing");
        }

        _position++;
        return inner;
    }

    private IRecordPredicate? Comparison()
    {
        var fieldName = ReadWord();
        if (fieldName.Length == 0)
        {
            return Fail($"a field name is expected at position {_position + 1}");
        }

        SkipSpaces();
        var comparator = TakeFrom(RecordPredicateKeywords.Comparators, requireWord: false);
        if (comparator is not null)
        {
            return Compare(fieldName, comparator);
        }

        var keyword = TakeFrom(RecordPredicateKeywords.TextWords, requireWord: true);
        if (keyword is not null)
        {
            var text = ReadValue(out _);
            return text is null ? null : new TextRecordPredicate(fieldName, keyword, text);
        }

        if (AtBoundary())
        {
            return new FieldRecordPredicate(fieldName);
        }

        return Fail($"unexpected '{Rest()}' after '{fieldName}' at position {_position + 1}");
    }

    private IRecordPredicate? Compare(string fieldName, string comparator)
    {
        var value = ReadValue(out var quoted);
        if (value is null)
        {
            return null;
        }

        var isEquality = comparator == RecordPredicateKeywords.Equal || comparator == RecordPredicateKeywords.NotEqual;
        if (isEquality && !quoted && string.Equals(value, RecordPredicateKeywords.NullWord, StringComparison.OrdinalIgnoreCase))
        {
            return new NullCheckRecordPredicate(fieldName, wantsNull: comparator == RecordPredicateKeywords.Equal);
        }

        return new ComparisonRecordPredicate(fieldName, comparator, value);
    }

    // A value is a quoted text, or the bare text up to the next 'and' / 'or' / '&&' / '||' or the parenthesis that
    // closes the group it is in. Bare text may hold spaces: a text parameter is placed into a condition that way.
    private string? ReadValue(out bool quoted)
    {
        quoted = false;
        SkipSpaces();
        if (_position >= _text.Length)
        {
            Fail("a value is expected after the comparison");
            return null;
        }

        var first = _text[_position];
        if (first == '\'' || first == '"')
        {
            quoted = true;
            return ReadQuoted(first);
        }

        return ReadBare();
    }

    private string? ReadQuoted(char quote)
    {
        var close = _text.IndexOf(quote, _position + 1);
        if (close < 0)
        {
            Fail($"the quote opened at position {_position + 1} is not closed");
            return null;
        }

        var value = _text.Substring(_position + 1, close - _position - 1);
        _position = close + 1;
        SkipSpaces();
        if (!AtBoundary())
        {
            Fail($"unexpected '{Rest()}' after the quoted text at position {_position + 1}");
            return null;
        }

        return value;
    }

    private string? ReadBare()
    {
        var depth = 0;
        var index = _position;
        while (index < _text.Length)
        {
            var current = _text[index];
            if (current == '(')
            {
                depth++;
            }
            else if (current == ')')
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
            }
            else if (char.IsWhiteSpace(current) && ConnectiveAhead(index))
            {
                break;
            }

            index++;
        }

        var value = _text.Substring(_position, index - _position).Trim();
        if (value.Length == 0)
        {
            Fail($"a value is expected at position {_position + 1}");
            return null;
        }

        _position = index;
        return value;
    }

    private bool TakeConnective(string word, string sign)
    {
        SkipSpaces();
        if (StartsAt(_position, sign))
        {
            _position += sign.Length;
            return true;
        }

        if (WordAt(_position, word))
        {
            _position += word.Length;
            return true;
        }

        return false;
    }

    private string? TakeFrom(string[] candidates, bool requireWord)
    {
        foreach (var candidate in candidates)
        {
            var matches = requireWord ? WordAt(_position, candidate) : StartsAt(_position, candidate);
            if (matches)
            {
                _position += candidate.Length;
                return candidate;
            }
        }

        return null;
    }

    private bool AtBoundary()
    {
        SkipSpaces();
        return _position >= _text.Length || _text[_position] == ')' || ConnectiveAt(_position);
    }

    private bool ConnectiveAhead(int index)
    {
        var next = index;
        while (next < _text.Length && char.IsWhiteSpace(_text[next]))
        {
            next++;
        }

        return ConnectiveAt(next);
    }

    private bool ConnectiveAt(int index) =>
        StartsAt(index, "&&") || StartsAt(index, "||")
        || WordAt(index, RecordPredicateKeywords.And) || WordAt(index, RecordPredicateKeywords.Or);

    private string ReadWord()
    {
        var start = _position;
        while (_position < _text.Length && !char.IsWhiteSpace(_text[_position]) && WordBreaks.IndexOf(_text[_position]) < 0)
        {
            _position++;
        }

        return _text.Substring(start, _position - start);
    }

    private void RequireEnd()
    {
        SkipSpaces();
        if (_position < _text.Length)
        {
            Fail($"unexpected '{Rest()}' at position {_position + 1}");
        }
    }

    private void SkipSpaces()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
        {
            _position++;
        }
    }

    private bool StartsAt(int index, string sign) =>
        index + sign.Length <= _text.Length && string.CompareOrdinal(_text, index, sign, 0, sign.Length) == 0;

    // A word, in any case, that is followed by a space, an opening parenthesis or the end of the text.
    private bool WordAt(int index, string word)
    {
        var end = index + word.Length;
        if (end > _text.Length || string.Compare(_text, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        return end == _text.Length || char.IsWhiteSpace(_text[end]) || _text[end] == '(' || _text[end] == '\'' || _text[end] == '"';
    }

    private string Rest()
    {
        var rest = _text.Substring(_position).Trim();
        return rest.Length <= 40 ? rest : string.Concat(rest.AsSpan(0, 40), "...");
    }

    private IRecordPredicate? Fail(string problem)
    {
        _problem ??= problem;
        return null;
    }
}
