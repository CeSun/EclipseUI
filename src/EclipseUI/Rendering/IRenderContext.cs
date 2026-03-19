namespace EclipseUI.Rendering;

/// <summary>
/// 渲染上下文接口 - 封装所有绘制操作
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// 清空画布
    /// </summary>
    void Clear(Color color);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    void DrawRectangle(float x, float y, float width, float height, IBrush? brush = null, IPen? pen = null);
    
    /// <summary>
    /// 绘制圆角矩形
    /// </summary>
    void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius, IBrush? brush = null, IPen? pen = null);
    
    /// <summary>
    /// 绘制文本（使用指定字体）
    /// </summary>
    void DrawText(string text, float x, float y, IFont font, Color color);
    
    /// <summary>
    /// 绘制文本（自动选择字体，支持 Emoji）
    /// </summary>
    void DrawText(string text, float x, float y, float fontSize, Color color);
    
    /// <summary>
    /// 绘制文本（直接指定字体名，用于 emoji 等特殊字体）
    /// </summary>
    void DrawTextDirect(string text, float x, float y, float fontSize, string fontFamily, Color color);
    
    /// <summary>
    /// 绘制图片
    /// </summary>
    void DrawImage(IImage image, float x, float y, float? width = null, float? height = null);
    
    /// <summary>
    /// 保存画布状态
    /// </summary>
    void Save();
    
    /// <summary>
    /// 恢复画布状态
    /// </summary>
    void Restore();
    
    /// <summary>
    /// 平移变换
    /// </summary>
    void Translate(float dx, float dy);
    
    /// <summary>
    /// 旋转变换
    /// </summary>
    void Rotate(float degrees);
    
    /// <summary>
    /// 缩放变换
    /// </summary>
    void Scale(float sx, float sy);
}

/// <summary>
/// 画刷接口
/// </summary>
public interface IBrush
{
    Color Color { get; set; }
}

/// <summary>
/// 画笔接口
/// </summary>
public interface IPen
{
    Color Color { get; set; }
    float StrokeWidth { get; set; }
}

/// <summary>
/// 字体接口
/// </summary>
public interface IFont
{
    string FamilyName { get; }
    float Size { get; }
    bool IsBold { get; }
    bool IsItalic { get; }
}

/// <summary>
/// 图片接口
/// </summary>
public interface IImage
{
    int Width { get; }
    int Height { get; }
}

/// <summary>
/// 颜色结构
/// </summary>
public readonly struct Color
{
    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color FromArgb(int alpha, int red, int green, int blue) 
        => new Color((byte)red, (byte)green, (byte)blue, (byte)alpha);
    
    public static Color FromRgb(int red, int green, int blue) 
        => new Color((byte)red, (byte)green, (byte)blue);
    
    public static Color Black => new Color(0, 0, 0);
    public static Color White => new Color(255, 255, 255);
    public static Color Red => new Color(255, 0, 0);
    public static Color Green => new Color(0, 255, 0);
    public static Color Blue => new Color(0, 0, 255);
    public static Color Transparent => new Color(0, 0, 0, 0);
}
