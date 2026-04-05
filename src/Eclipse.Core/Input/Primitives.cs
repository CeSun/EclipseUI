using System;

namespace Eclipse.Input;

/// <summary>
/// 二维点
/// </summary>
public readonly struct Point
{
    public double X { get; init; }
    public double Y { get; init; }
    
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public static Point Zero => new(0, 0);
    
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator *(Point p, double scalar) => new(p.X * scalar, p.Y * scalar);
    
    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSquared => X * X + Y * Y;
    
    public override string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// 二维向量
/// </summary>
public readonly struct Vector
{
    public double X { get; init; }
    public double Y { get; init; }
    
    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public static Vector Zero => new(0, 0);
    
    public double Length => Math.Sqrt(X * X + Y * Y);
}

/// <summary>
/// 尺寸结构
/// </summary>
public readonly struct Size
{
    public double Width { get; init; }
    public double Height { get; init; }
    
    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }
    
    public static Size Empty => new(double.PositiveInfinity, double.PositiveInfinity);
    public static Size Zero => new(0, 0);
    
    public bool IsEmpty => Width == double.PositiveInfinity || Height == double.PositiveInfinity;
    
    public override string ToString() => $"({Width:F1}, {Height:F1})";
}

/// <summary>
/// 矩形
/// </summary>
public readonly struct Rect
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    
    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    
    public bool Contains(Point point)
    {
        return point.X >= X && point.X <= Right &&
               point.Y >= Y && point.Y <= Bottom;
    }
    
    public static Rect Empty => new(0, 0, 0, 0);
    
    public bool IsEmpty => Width <= 0 || Height <= 0;
}