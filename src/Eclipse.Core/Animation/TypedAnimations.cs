using System;
using Eclipse.Rendering;

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
        public Color From { get; set; }
        public Color To { get; set; }
        public Color CurrentValue { get; private set; }
        
        public event EventHandler<Color>? ValueChanged;
        
        public ColorAnimation(Color from, Color to, double duration = 1.0)
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
        
        private static Color InterpolateColor(Color from, Color to, double t)
        {
            return new Color(
                (byte)(from.R + (to.R - from.R) * t),
                (byte)(from.G + (to.G - from.G) * t),
                (byte)(from.B + (to.B - from.B) * t),
                (byte)(from.A + (to.A - from.A) * t)
            );
        }
        
        public override void Reset()
        {
            base.Reset();
            CurrentValue = From;
        }
    }
}
