using System;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// A statement prepared with sp_prepare or sp_prepexec: its text and the parameters it declares, run
/// again by handle with sp_execute.
/// </summary>
public sealed class PreparedStatement
{
    /// <summary>Initializes a new instance of the <see cref="PreparedStatement"/> class.</summary>
    /// <param name="parameterDeclarations">The parameter list as declared, such as "@p0 int, @p1 nvarchar(10)".</param>
    /// <param name="text">The statement text.</param>
    public PreparedStatement(string parameterDeclarations, string text)
    {
        ParameterDeclarations = parameterDeclarations ?? throw new ArgumentNullException(nameof(parameterDeclarations));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the parameter list as declared.</summary>
    public string ParameterDeclarations { get; }

    /// <summary>Gets the statement text.</summary>
    public string Text { get; }
}
