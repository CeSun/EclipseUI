using System;
using System.Globalization;

namespace Eclipse.Rendering;

/// <summary>
/// 表示 RGBA 颜色值
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>
    /// 红色分量 (0-255)
    /// </summary>
    public byte R { get; }
    
    /// <summary>
    /// 绿色分量 (0-255)
    /// </summary>
    public byte G { get; }
    
    /// <summary>
    /// 蓝色分量 (0-255)
    /// </summary>
    public byte B { get; }
    
    /// <summary>
    /// Alpha 分量 (0-255)
    /// </summary>
    public byte A { get; }
    
    /// <summary>
    /// 创建颜色
    /// </summary>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    // === 预定义颜色 ===
    
    /// <summary>透明</summary>
    public static Color Transparent => new(0, 0, 0, 0);
    
    /// <summary>白色</summary>
    public static Color White => new(255, 255, 255);
    
    /// <summary>黑色</summary>
    public static Color Black => new(0, 0, 0);
    
    /// <summary>红色</summary>
    public static Color Red => new(255, 0, 0);
    
    /// <summary>绿色</summary>
    public static Color Green => new(0, 128, 0);
    
    /// <summary>蓝色</summary>
    public static Color Blue => new(0, 0, 255);
    
    /// <summary>灰色</summary>
    public static Color Gray => new(128, 128, 128);
    
    /// <summary>浅灰色</summary>
    public static Color LightGray => new(192, 192, 192);
    
    /// <summary>深灰色</summary>
    public static Color DarkGray => new(64, 64, 64);
    
    /// <summary>橙色</summary>
    public static Color Orange => new(255, 165, 0);
    
    /// <summary>黄色</summary>
    public static Color Yellow => new(255, 255, 0);
    
    /// <summary>青色</summary>
    public static Color Cyan => new(0, 255, 255);
    
    /// <summary>品红色</summary>
    public static Color Magenta => new(255, 0, 255);
    
    /// <summary>系统蓝色 (iOS #007AFF)</summary>
    public static Color SystemBlue => new(0, 122, 255);
    
    /// <summary>系统绿色</summary>
    public static Color SystemGreen => new(52, 199, 89);
    
    /// <summary>系统红色</summary>
    public static Color SystemRed => new(255, 59, 48);
    
    /// <summary>系统橙色</summary>
    public static Color SystemOrange => new(255, 149, 0);
    
    /// <summary>分隔线颜色</summary>
    public static Color Separator => new(0, 0, 0, 128);
    
    // === 工厂方法 ===
    
    /// <summary>
    /// 从 RGB 值创建颜色
    /// </summary>
    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);
    
    /// <summary>
    /// 从 RGBA 值创建颜色
    /// </summary>
    public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);
    
    /// <summary>
    /// 从 ARGB 值创建颜色
    /// </summary>
    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);
    
    /// <summary>
    /// 从 HSL 值创建颜色
    /// </summary>
    /// <param name="h">色相 (0-360)</param>
    /// <param name="s">饱和度 (0-1)</param>
    /// <param name="l">亮度 (0-1)</param>
    public static Color FromHsl(double h, double s, double l)
    {
        if (s == 0)
        {
            var gray = (byte)(l * 255);
            return new Color(gray, gray, gray);
        }
        
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;
        
        byte r = (byte)(255 * HueToRgb(p, q, h / 360 + 1.0 / 3));
        byte g = (byte)(255 * HueToRgb(p, q, h / 360));
        byte b = (byte)(255 * HueToRgb(p, q, h / 360 - 1.0 / 3));
        
        return new Color(r, g, b);
    }
    
    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
    
    /// <summary>
    /// 从十六进制字符串解析颜色
    /// 支持格式: #RGB, #RGBA, #RRGGBB, #RRGGBBAA, RGB, RGBA, RRGGBB, RRGGBBAA
    /// </summary>
    public static Color Parse(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return Transparent;
        
        var span = hexColor.AsSpan().Trim();
        
        // 移除 # 前缀
        if (span.StartsWith("#"))
            span = span.Slice(1);
        
        // 移除 0x 前缀
        if (span.StartsWith("0x") || span.StartsWith("0X"))
            span = span.Slice(2);
        
        byte r, g, b, a = 255;
        
        switch (span.Length)
        {
            case 3: // RGB
                r = ParseHexByte(span[0], span[0]);
                g = ParseHexByte(span[1], span[1]);
                b = ParseHexByte(span[2], span[2]);
                break;
            case 4: // RGBA
                r = ParseHexByte(span[0], span[0]);
                g = ParseHexByte(span[1], span[1]);
                b = ParseHexByte(span[2], span[2]);
                a = ParseHexByte(span[3], span[3]);
                break;
            case 6: // RRGGBB
                r = ParseHexByte(span[0], span[1]);
                g = ParseHexByte(span[2], span[3]);
                b = ParseHexByte(span[4], span[5]);
                break;
            case 8: // RRGGBBAA
                r = ParseHexByte(span[0], span[1]);
                g = ParseHexByte(span[2], span[3]);
                b = ParseHexByte(span[4], span[5]);
                a = ParseHexByte(span[6], span[7]);
                break;
            default:
                return Transparent;
        }
        
        return new Color(r, g, b, a);
    }
    
    private static byte ParseHexByte(char c1, char c2)
    {
        int value = (HexValue(c1) << 4) | HexValue(c2);
        return (byte)value;
    }
    
    private static int HexValue(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        return 0;
    }
    
    /// <summary>
    /// 尝试解析十六进制字符串为颜色
    /// </summary>
    public static bool TryParse(string? hexColor, out Color color)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            color = Transparent;
            return false;
        }
        
        try
        {
            color = Parse(hexColor);
            return true;
        }
        catch
        {
            color = Transparent;
            return false;
        }
    }
    
    // === 实例方法 ===
    
    /// <summary>
    /// 返回带指定透明度的颜色
    /// </summary>
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);
    
    /// <summary>
    /// 返回带指定透明度的颜色 (0-1 范围)
    /// </summary>
    public Color WithAlpha(double alpha) => new(R, G, B, (byte)(alpha * 255));
    
    /// <summary>
    /// 返回更亮的颜色
    /// </summary>
    public Color Lighten(double amount = 0.1)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)Math.Min(255, R + (255 - R) * amount);
        byte g = (byte)Math.Min(255, G + (255 - G) * amount);
        byte b = (byte)Math.Min(255, B + (255 - B) * amount);
        return new Color(r, g, b, A);
    }
    
    /// <summary>
    /// 返回更暗的颜色
    /// </summary>
    public Color Darken(double amount = 0.1)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)(R * (1 - amount));
        byte g = (byte)(G * (1 - amount));
        byte b = (byte)(B * (1 - amount));
        return new Color(r, g, b, A);
    }
    
    /// <summary>
    /// 转换为十六进制字符串
    /// </summary>
    public string ToHex()
    {
        if (A == 255)
            return $"#{R:X2}{G:X2}{B:X2}";
        return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }
    
    /// <summary>
    /// 转换为 RGBA 字符串
    /// </summary>
    public string ToRgba()
    {
        return $"rgba({R}, {G}, {B}, {A / 255.0:0.##})";
    }
    
    // === IEquatable<Color> ===
    
    public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
    
    public override bool Equals(object? obj) => obj is Color other && Equals(other);
    
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
    
    public static bool operator ==(Color left, Color right) => left.Equals(right);
    
    public static bool operator !=(Color left, Color right) => !left.Equals(right);
    
    // === 隐式转换 ===
    
    /// <summary>
    /// 从十六进制字符串隐式转换
    /// </summary>
    public static implicit operator Color(string? hexColor) => 
        string.IsNullOrEmpty(hexColor) ? Transparent : Parse(hexColor);
    
    /// <summary>
    /// 从 uint (ARGB) 隐式转换
    /// </summary>
    public static implicit operator Color(uint argb) => 
        new((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF), (byte)((argb >> 24) & 0xFF));
    
    // === ToString ===
    
    public override string ToString() => ToHex();
}