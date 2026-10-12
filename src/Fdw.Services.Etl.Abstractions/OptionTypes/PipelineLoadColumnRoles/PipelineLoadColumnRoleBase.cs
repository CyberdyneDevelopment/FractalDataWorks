using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="PipelineLoadColumnRoles"/>, using the CRTP pattern.
/// </summary>
public abstract class PipelineLoadColumnRoleBase : TypeOptionBase<PipelineLoadColumnRoleBase>, IPipelineLoadColumnRole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineLoadColumnRoleBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="belongsToValueSource">Whether the role is only valid on a value source (SequenceOrder) rather than on a load step.</param>
    /// <param name="isMatchKey">Whether the role makes the column part of the key rows are matched on (Key).</param>
    /// <param name="isComparison">Whether the role makes the column one whose difference means a matched row changed (Compare).</param>
    /// <param name="isWritten">Whether the role makes the column one the load writes (Write).</param>
    protected PipelineLoadColumnRoleBase(string name, bool belongsToValueSource, bool isMatchKey, bool isComparison, bool isWritten) : base(name, "PipelineLoadColumnRoles")
    {
        BelongsToValueSource = belongsToValueSource;
        IsMatchKey = isMatchKey;
        IsComparison = isComparison;
        IsWritten = isWritten;
    }

    /// <inheritdoc/>
    public bool BelongsToValueSource { get; }

    /// <inheritdoc/>
    public bool IsMatchKey { get; }

    /// <inheritdoc/>
    public bool IsComparison { get; }

    /// <inheritdoc/>
    public bool IsWritten { get; }
}
