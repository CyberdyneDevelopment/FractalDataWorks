using System.Linq;

namespace Fdw.ApiSurface;

/// <summary>Joins the non-empty words of a detail with one space between them.</summary>
internal static class Words
{
    public static string Join(params string[] parts)
    {
        return string.Join(' ', parts.Where(part => part.Length > 0));
    }
}
