namespace Fdw.UI.Components.Services;

/// <summary>
/// Chooses the badge tone an http method is drawn in.
/// </summary>
/// <remarks>
/// A verb is not a status, so it is not a member of <see cref="StatusVariants"/> — but it is drawn in
/// the same five tones, and the page that draws it should not be the place that decides which. Read a
/// request the way an operator reads it: the write that creates takes the success tone, the
/// destructive one takes the error tone, and the rest sit between.
/// </remarks>
public static class HttpMethodBadge
{
    /// <summary>
    /// Gets the tone for an http method.
    /// </summary>
    /// <param name="method">The method name, in any casing.</param>
    /// <returns>The variant the pill is drawn in.</returns>
    public static StatusVariantBase Variant(string method) =>
        method.ToUpperInvariant() switch
        {
            "POST" => StatusVariants.Success,
            "PUT" => StatusVariants.Info,
            "PATCH" => StatusVariants.Warning,
            "DELETE" => StatusVariants.Error,
            _ => StatusVariants.Neutral,
        };
}
