using System;
using System.Runtime.InteropServices;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Represents a rectangle for bounding boxes.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Rect : IEquatable<Rect>
{
    /// <summary>
    /// Gets the X coordinate of the left edge.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y coordinate of the top edge.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the width.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Gets the height.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rect"/> struct.
    /// </summary>
    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets an empty rectangle.
    /// </summary>
    public static Rect Empty => new Rect(0, 0, 0, 0);

    /// <summary>
    /// Gets the right edge X coordinate.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Gets the bottom edge Y coordinate.
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Gets the center point.
    /// </summary>
    public Point Center => new Point(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// Determines if this rectangle contains a point.
    /// </summary>
    public bool Contains(Point point) =>
        point.X >= X && point.X <= Right &&
        point.Y >= Y && point.Y <= Bottom;

    /// <summary>
    /// Creates a rectangle from an origin point and size.
    /// </summary>
    public static Rect FromPointAndSize(Point origin, double width, double height) =>
        new Rect(origin.X, origin.Y, width, height);

    /// <summary>
    /// Creates a rectangle from two corner points.
    /// </summary>
    public static Rect FromCorners(Point topLeft, Point bottomRight) =>
        new Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

    /// <summary>
    /// Determines if this rectangle intersects with another.
    /// </summary>
    public bool Intersects(Rect other) =>
        X < other.Right && Right > other.X &&
        Y < other.Bottom && Bottom > other.Y;

    /// <summary>
    /// Returns a rectangle inflated by the given amount on all sides.
    /// </summary>
    public Rect Inflate(double amount) =>
        new Rect(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    /// <summary>
    /// Returns the smallest rectangle that contains both this and another rectangle.
    /// </summary>
    public Rect Union(Rect other)
    {
        var x = Math.Min(X, other.X);
        var y = Math.Min(Y, other.Y);
        var right = Math.Max(Right, other.Right);
        var bottom = Math.Max(Bottom, other.Bottom);
        return new Rect(x, y, right - x, bottom - y);
    }

    /// <inheritdoc/>
    public bool Equals(Rect other) =>
        X.Equals(other.X) && Y.Equals(other.Y) &&
        Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Rect other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Width.GetHashCode();
            hash = hash * 31 + Height.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Determines whether two rectangles are equal.
    /// </summary>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>
    /// Determines whether two rectangles are not equal.
    /// </summary>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"({X}, {Y}, {Width}, {Height})";
}
