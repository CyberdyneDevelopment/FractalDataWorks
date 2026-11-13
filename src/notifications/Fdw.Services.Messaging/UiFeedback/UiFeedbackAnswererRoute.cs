namespace Fdw.Services.Messaging;

/// <summary>
/// One entry of the <c>UiFeedbackQuestionAnswerers</c> setting: the holders of <see cref="Permission"/> answer the questions left on the screens whose route
/// starts with <see cref="RoutePrefix"/>.
/// </summary>
internal sealed class UiFeedbackAnswererRoute
{
    /// <summary>Gets or sets the route prefix, such as <c>/documents</c>.</summary>
    public string? RoutePrefix { get; set; }

    /// <summary>Gets or sets the permission whose holders answer, such as <c>document-setup:write</c>.</summary>
    public string? Permission { get; set; }
}
