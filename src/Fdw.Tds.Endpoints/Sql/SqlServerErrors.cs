using System;
using Fdw.Protocols.Tds;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// The SQL Server errors this endpoint raises, each with the number, class and text SQL Server itself
/// uses for the same refusal.
/// </summary>
/// <remarks>
/// Clients act on the number — a driver retries some, a tool reports 208 as a missing table — so a
/// refusal that has a SQL Server equivalent is raised as that equivalent, never as a generic error.
/// </remarks>
public static class SqlServerErrors
{
    /// <summary>18456: the login failed. The same for an unknown login and a wrong password.</summary>
    /// <param name="userName">The login name presented.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage LoginFailed(string userName)
        => new(18456, 1, 14, $"Login failed for user '{userName}'.", 1);

    /// <summary>4060: the database the login named cannot be opened.</summary>
    /// <param name="database">The database named.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage CannotOpenDatabase(string database)
        => new(4060, 1, 11, $"Cannot open database \"{database}\" requested by the login. The login failed.", 1);

    /// <summary>208: no object by that name, or none the caller may see.</summary>
    /// <param name="name">The name as written.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage InvalidObjectName(string name, int line)
        => new(208, 1, 16, $"Invalid object name '{name}'.", line);

    /// <summary>207: no column by that name.</summary>
    /// <param name="name">The name as written.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage InvalidColumnName(string name, int line)
        => new(207, 1, 16, $"Invalid column name '{name}'.", line);

    /// <summary>102: the batch does not parse.</summary>
    /// <param name="parserMessage">What the parser reported.</param>
    /// <param name="line">The line it reported.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage Syntax(string parserMessage, int line)
        => new(102, 1, 15, parserMessage, line);

    /// <summary>40517: a statement or option this server does not support.</summary>
    /// <param name="what">What was asked for.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage NotSupported(string what, int line)
        => new(40517, 1, 16, $"Keyword or statement option '{what}' is not supported in this version of SQL Server.", line);

    /// <summary>3906: a write against a read-only database.</summary>
    /// <param name="database">The database.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage ReadOnly(string database, int line)
        => new(3906, 1, 16, $"Failed to update database \"{database}\" because the database is read-only.", line);

    /// <summary>2812: no stored procedure by that name.</summary>
    /// <param name="name">The name as written.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage ProcedureNotFound(string name, int line)
        => new(2812, 62, 16, $"Could not find stored procedure '{name}'.", line);

    /// <summary>137: a variable was used without being declared.</summary>
    /// <param name="name">The variable's name.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage MustDeclareVariable(string name, int line)
        => new(137, 2, 15, $"Must declare the scalar variable \"{name}\".", line);

    /// <summary>195: a function name this server does not know.</summary>
    /// <param name="name">The function's name.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage UnknownFunction(string name, int line)
        => new(195, 10, 15, $"'{name}' is not a recognized built-in function name.", line);

    /// <summary>245: a value does not convert to the type asked for.</summary>
    /// <param name="target">The type it was converted to.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage ConversionFailed(string target, int line)
        => new(245, 1, 16, $"Conversion failed when converting a value to data type {target}.", line);

    /// <summary>8134: division by zero.</summary>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage DivideByZero(int line)
        => new(8134, 1, 16, "Divide by zero error encountered.", line);

    /// <summary>8144 and its kin: a procedure called with parameters it does not take.</summary>
    /// <param name="procedure">The procedure.</param>
    /// <param name="reason">What is wrong with the parameters.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage BadParameters(string procedure, string reason)
        => new(8144, 2, 16, $"Procedure or function {procedure} has invalid parameters: {reason}.", 1);

    /// <summary>217: procedures and dynamic SQL nested past SQL Server's limit.</summary>
    /// <param name="limit">The nesting limit.</param>
    /// <param name="line">The line of the call that would go past it.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage NestingTooDeep(int limit, int line)
        => new(217, 1, 16, $"Maximum stored procedure, function, trigger, or view nesting level exceeded (limit {limit}).", line);

    /// <summary>8179: an sp_execute handle that was never prepared or was unprepared.</summary>
    /// <param name="handle">The handle.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage UnknownPreparedHandle(int handle)
        => new(8179, 5, 16, $"Could not find prepared statement with handle {handle}.", 1);

    /// <summary>50000: a failure from the data platform behind this server, named by its code alone.</summary>
    /// <remarks>
    /// The platform's own message stays in this server's log, where it was written when the failure was made. It can
    /// carry what a caller from outside must not see: a store's raw error naming physical tables, servers or logins,
    /// or why an access check could not decide.
    /// </remarks>
    /// <param name="failure">The failed result.</param>
    /// <param name="line">The line of the statement that failed.</param>
    /// <returns>The error.</returns>
    public static SqlServerMessage PlatformFailure(IGenericResult failure, int line)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return failure.Code is { } code
            ? new SqlServerMessage(50000, 1, 16, $"The request could not be completed ({code.Code}).", line)
            : new SqlServerMessage(50000, 1, 16, "The request could not be completed.", line);
    }
}
