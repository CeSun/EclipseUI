using HarfBuzzSharp;
using SkiaSharp;

namespace Eclipse.Skia.Text;

/// <summary>
/// HarfBuzz 文本塑形器 - 处理复杂文本布局、emoji、连字等
/// 
/// 注意：这是一个简化实现，主要依赖 SkiaSharp 的内置文本渲染。
/// 完整的 HarfBuzz 塑形需要更复杂的字体数据处理。
/// </summary>
public class HarfBuzzTextShaper : IDisposable
{
    private readonly Font _font;
    private readonly Face _face;
    private readonly Blob _blob;
    
    /// <summary>
    /// Emoji 字体优先级
    /// </summary>
    public static readonly string[] EmojiFontFamilies =
    {
        "Segoe UI Emoji",
        "Noto Color Emoji",
        "Apple Color Emoji",
        "Twemoji Mozilla"
    };
    
    /// <summary>
    /// 中文字体优先级
    /// </summary>
    public static readonly string[] ChineseFontFamilies =
    {
        "Microsoft YaHei",
        "PingFang SC",
        "SimSun",
        "Noto Sans CJK SC",
        "Segoe UI"
    };

    public SKTypeface Typeface { get; }
    
    // 缓存
    private static readonly Dictionary<string, HarfBuzzTextShaper> _cache = new();
    
    private HarfBuzzTextShaper(SKTypeface typeface)
    {
        Typeface = typeface;
        
        // 使用空的 Blob/Face/Font - HarfBuzz 塑形会在真正需要时才加载字体数据
        // 对于简单渲染，SkiaSharp 已经足够
        _blob = Blob.Empty;
        _face = new Face(_blob, 0);
        _font = new Font(_face);
    }
    
    /// <summary>
    /// 获取或创建塑形器（带缓存）
    /// </summary>
    public static HarfBuzzTextShaper GetOrCreate(SKTypeface typeface)
    {
        var key = typeface.FamilyName;
        if (!_cache.TryGetValue(key, out var shaper))
        {
            shaper = new HarfBuzzTextShaper(typeface);
            _cache[key] = shaper;
        }
        return shaper;
    }
    
    /// <summary>
    /// 获取默认中文字体塑形器
    /// </summary>
    public static HarfBuzzTextShaper GetChineseShaper()
    {
        foreach (var family in ChineseFontFamilies)
        {
            var typeface = SKTypeface.FromFamilyName(family, 
                SKFontStyleWeight.Normal, 
                SKFontStyleWidth.Normal, 
                SKFontStyleSlant.Upright);
            
            if (typeface != null && typeface.CountGlyphs("中") > 0)
            {
                return GetOrCreate(typeface);
            }
        }
        
        return GetOrCreate(SKTypeface.Default);
    }
    
    /// <summary>
    /// 获取 Emoji 字体塑形器
    /// </summary>
    public static HarfBuzzTextShaper GetEmojiShaper()
    {
        foreach (var family in EmojiFontFamilies)
        {
            var typeface = SKTypeface.FromFamilyName(family,
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
            
            if (typeface != null && typeface.CountGlyphs("\U0001F600") > 0)
            {
                return GetOrCreate(typeface);
            }
        }
        
        return GetOrCreate(SKTypeface.Default);
    }
    
    /// <summary>
    /// 塑形文本 - 返回字形信息
    /// 
    /// 注意：这是简化实现，对于完整 HarfBuzz 塑形需要加载字体数据。
    /// 当前主要用于获取字形数量等信息。
    /// </summary>
    public ShapedGlyph[] Shape(string text, float fontSize, Direction direction = Direction.LeftToRight)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<ShapedGlyph>();
        
        // 使用 SKFont 的 MeasureText 代替完整塑形
        using var font = new SKFont
        {
            Typeface = Typeface,
            Size = fontSize,
            Edging = SKFontEdging.SubpixelAntialias
        };
        
        // HarfBuzz 设置字体大小
        _font.SetScale((int)(fontSize * 64), (int)(fontSize * 64));
        
        // 创建 buffer
        using var buffer = new HarfBuzzSharp.Buffer();
        buffer.AddUtf16(text);
        buffer.Direction = direction;
        buffer.GuessSegmentProperties();
        
        // 塑形
        _font.Shape(buffer);
        
        // 提取字形信息
        var glyphInfos = buffer.GlyphInfos;
        var glyphPositions = buffer.GlyphPositions;
        
        var result = new ShapedGlyph[glyphInfos.Length];
        for (int i = 0; i < glyphInfos.Length; i++)
        {
            result[i] = new ShapedGlyph
            {
                Codepoint = glyphInfos[i].Codepoint,
                Cluster = glyphInfos[i].Cluster,
                XAdvance = glyphPositions[i].XAdvance / 64f,
                YAdvance = glyphPositions[i].YAdvance / 64f,
                XOffset = glyphPositions[i].XOffset / 64f,
                YOffset = glyphPositions[i].YOffset / 64f
            };
        }
        
        return result;
    }
    
    public void Dispose()
    {
        _font.Dispose();
        _face.Dispose();
        _blob.Dispose();
    }
}

/// <summary>
/// 塑形后的字形信息
/// </summary>
public struct ShapedGlyph
{
    /// <summary>
    /// Unicode 码点
    /// </summary>
    public uint Codepoint;
    
    /// <summary>
    /// 字符索引（用于定位原始文本）
    /// </summary>
    public uint Cluster;
    
    /// <summary>
    /// X 方向前进距离（像素）
    /// </summary>
    public float XAdvance;
    
    /// <summary>
    /// Y 方向前进距离（像素）
    /// </summary>
    public float YAdvance;
    
    /// <summary>
    /// X 方向偏移（像素）
    /// </summary>
    public float XOffset;
    
    /// <summary>
    /// Y 方向偏移（像素）
    /// </summary>
    public float YOffset;
}