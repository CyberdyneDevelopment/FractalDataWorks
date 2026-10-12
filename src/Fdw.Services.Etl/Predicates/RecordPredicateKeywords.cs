namespace Fdw.Services.Etl.Predicates;

/// <summary>The words and signs of the condition language, in one place.</summary>
internal static class RecordPredicateKeywords
{
    public const string Equal = "==";
    public const string NotEqual = "!=";
    public const string GreaterOrEqual = ">=";
    public const string LessOrEqual = "<=";
    public const string Greater = ">";
    public const string Less = "<";
    public const string Contains = "contains";
    public const string StartsWith = "startswith";
    public const string EndsWith = "endswith";
    public const string And = "and";
    public const string Or = "or";
    public const string Not = "not";
    public const string NullWord = "null";

    /// <summary>The comparison signs, longest first so that <c>&gt;=</c> is read before <c>&gt;</c>.</summary>
    public static readonly string[] Comparators = [GreaterOrEqual, LessOrEqual, Equal, NotEqual, Greater, Less];

    /// <summary>The words that compare a field's text.</summary>
    public static readonly string[] TextWords = [Contains, StartsWith, EndsWith];
}
