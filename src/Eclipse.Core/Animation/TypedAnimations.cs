using System;

namespace Eclipse.Core.Animation
{
    /// <summary>
    /// 双精度动画，用于动画化数值属性（如透明度、宽度、位置等）
    /// </summary>
    public class DoubleAnimation : AnimationBase
    {
        /// <summary>
        /// 起始值
        /// </summary>
        public double From { get; set; }
        
        /// <summary>
        /// 结束值
        /// </summary>
        public double To { get; set; }
        
        /// <summary>
        /// 当前值
        /// </summary>
        public double CurrentValue { get; private set; }
        
        /// <summary>
        /// 值变化事件
        /// </summary>
        public event EventHandler<double>? ValueChanged;
        
        public DoubleAnimation(double from = 0, double to = 1, double duration = 1.0)
            : base(duration)
        {
            From = from;
            To = to;
            CurrentValue = from;
        }
        
        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
            
            // 计算当前值
            var easedProgress = GetEasedProgress();
            CurrentValue = From + (To - From) * easedProgress;
            
            ValueChanged?.Invoke(this, CurrentValue);
        }
        
        public override void Reset()
        {
            base.Reset();
            CurrentValue = From;
        }
    }
    
    /// <summary>
    /// 颜色动画，用于动画化颜色属性
    /// </summary>
    public class ColorAnimation : AnimationBase
    {
        public SKColor From { get; set; }
        public SKColor To { get; set; }
        public SKColor CurrentValue { get; private set; }
        
        public event EventHandler<SKColor>? ValueChanged;
        
        public ColorAnimation(SKColor from, SKColor to, double duration = 1.0)
            : base(duration)
        {
            From = from;
            To = to;
            CurrentValue = from;
        }
        
        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
            
            var easedProgress = GetEasedProgress();
            CurrentValue = InterpolateColor(From, To, easedProgress);
            
            ValueChanged?.Invoke(this, CurrentValue);
        }
        
        private static SKColor InterpolateColor(SKColor from, SKColor to, double t)
        {
            return new SKColor(
                (byte)(from.Red + (to.Red - from.Red) * t),
                (byte)(from.Green + (to.Green - from.Green) * t),
                (byte)(from.Blue + (to.Blue - from.Blue) * t),
                (byte)(from.Alpha + (to.Alpha - from.Alpha) * t)
            );
        }
        
        public override void Reset()
        {
            base.Reset();
            CurrentValue = From;
        }
    }
    
    // 简单颜色结构（避免依赖 SkiaSharp）
    public readonly struct SKColor
    {
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }
        
        public SKColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
        
        public static SKColor Transparent => new(0, 0, 0, 0);
        public static SKColor Black => new(0, 0, 0);
        public static SKColor White => new(255, 255, 255);
        public static SKColor ColorRed => new(255, 0, 0);
        public static SKColor ColorGreen => new(0, 255, 0);
        public static SKColor ColorBlue => new(0, 0, 255);
        
        public SKColor WithAlpha(byte alpha) => new(Red, Green, Blue, alpha);
    }
}
