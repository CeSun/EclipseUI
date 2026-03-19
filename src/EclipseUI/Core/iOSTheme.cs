using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// iOS 风格主题常量
/// </summary>
public static class iOSTheme
{
    // 主色调
    public static readonly SKColor SystemBlue = SKColor.Parse("#007AFF");
    public static readonly SKColor SystemGreen = SKColor.Parse("#34C759");
    public static readonly SKColor SystemRed = SKColor.Parse("#FF3B30");
    public static readonly SKColor SystemOrange = SKColor.Parse("#FF9500");
    public static readonly SKColor SystemYellow = SKColor.Parse("#FFCC00");
    public static readonly SKColor SystemPurple = SKColor.Parse("#AF52DE");
    public static readonly SKColor SystemPink = SKColor.Parse("#FF2D55");
    public static readonly SKColor SystemTeal = SKColor.Parse("#5AC8FA");
    public static readonly SKColor SystemIndigo = SKColor.Parse("#5856D6");
    
    // 灰度
    public static readonly SKColor SystemGray = SKColor.Parse("#8E8E93");
    public static readonly SKColor SystemGray2 = SKColor.Parse("#AEAEB2");
    public static readonly SKColor SystemGray3 = SKColor.Parse("#C7C7CC");
    public static readonly SKColor SystemGray4 = SKColor.Parse("#D1D1D6");
    public static readonly SKColor SystemGray5 = SKColor.Parse("#E5E5EA");
    public static readonly SKColor SystemGray6 = SKColor.Parse("#F2F2F7");
    
    // 背景色
    public static readonly SKColor BackgroundPrimary = SKColors.White;
    public static readonly SKColor BackgroundSecondary = SKColor.Parse("#F2F2F7");
    public static readonly SKColor BackgroundTertiary = SKColors.White;
    
    // 文字颜色
    public static readonly SKColor LabelPrimary = SKColors.Black;
    public static readonly SKColor LabelSecondary = SKColor.Parse("#3C3C43").WithAlpha(153); // 60%
    public static readonly SKColor LabelTertiary = SKColor.Parse("#3C3C43").WithAlpha(76);  // 30%
    public static readonly SKColor LabelQuaternary = SKColor.Parse("#3C3C43").WithAlpha(46); // 18%
    
    // 分隔线
    public static readonly SKColor Separator = SKColor.Parse("#3C3C43").WithAlpha(73); // 29%
    public static readonly SKColor SeparatorOpaque = SKColor.Parse("#C6C6C8");
    
    // 圆角
    public const float CornerRadiusSmall = 6f;
    public const float CornerRadiusMedium = 10f;
    public const float CornerRadiusLarge = 14f;
    public const float CornerRadiusXLarge = 20f;
    
    // 字体大小
    public const float FontSizeLargeTitle = 34f;
    public const float FontSizeTitle1 = 28f;
    public const float FontSizeTitle2 = 22f;
    public const float FontSizeTitle3 = 20f;
    public const float FontSizeHeadline = 17f;
    public const float FontSizeBody = 17f;
    public const float FontSizeCallout = 16f;
    public const float FontSizeSubheadline = 15f;
    public const float FontSizeFootnote = 13f;
    public const float FontSizeCaption1 = 12f;
    public const float FontSizeCaption2 = 11f;
    
    // 控件尺寸
    public const float ButtonHeight = 50f;
    public const float ButtonMinWidth = 64f;
    public const float SwitchWidth = 51f;
    public const float SwitchHeight = 31f;
    public const float SliderTrackHeight = 4f;
    public const float SliderThumbSize = 28f;
    public const float CheckboxSize = 22f;
    
    /// <summary>
    /// 获取按下状态的颜色（变暗）
    /// </summary>
    public static SKColor GetPressedColor(SKColor color)
    {
        return new SKColor(
            (byte)(color.Red * 0.8f),
            (byte)(color.Green * 0.8f),
            (byte)(color.Blue * 0.8f),
            color.Alpha
        );
    }
    
    /// <summary>
    /// 获取禁用状态的颜色
    /// </summary>
    public static SKColor GetDisabledColor(SKColor color)
    {
        return color.WithAlpha(76); // 30%
    }
}
