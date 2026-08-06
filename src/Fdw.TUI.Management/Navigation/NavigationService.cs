using System;
using System.Collections.Generic;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Stack-based navigation service for managing screens.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Stack<IScreen> _stack = new();

    /// <inheritdoc />
    public IScreen? Current => _stack.Count > 0 ? _stack.Peek() : null;

    /// <inheritdoc />
    public bool HasScreens => _stack.Count > 0;

    /// <inheritdoc />
    public int Depth => _stack.Count;

    /// <inheritdoc />
    public void Push(IScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _stack.Push(screen);
    }

    /// <inheritdoc />
    public IScreen? Pop()
    {
        return _stack.Count > 0 ? _stack.Pop() : null;
    }

    /// <inheritdoc />
    public void Replace(IScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        if (_stack.Count > 0)
        {
            _stack.Pop();
        }
        _stack.Push(screen);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _stack.Clear();
    }
}
