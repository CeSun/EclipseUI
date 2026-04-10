using System;

namespace Eclipse.Core.Animation
{
    /// <summary>
    /// 缓动函数类型
    /// </summary>
    public enum EasingType
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce
    }
    
    /// <summary>
    /// 缓动函数
    /// </summary>
    public class EasingFunction
    {
        public EasingType Type { get; }
        
        public EasingFunction(EasingType type = EasingType.Linear)
        {
            Type = type;
        }
        
        /// <summary>
        /// 应用缓动函数
        /// </summary>
        /// <param name="t">进度值 (0-1)</param>
        /// <returns>缓动后的值</returns>
        public double Apply(double t)
        {
            return Type switch
            {
                EasingType.Linear => LinearFunc(t),
                EasingType.EaseInQuad => EaseInQuad(t),
                EasingType.EaseOutQuad => EaseOutQuad(t),
                EasingType.EaseInOutQuad => EaseInOutQuad(t),
                EasingType.EaseInCubic => EaseInCubic(t),
                EasingType.EaseOutCubic => EaseOutCubic(t),
                EasingType.EaseInOutCubic => EaseInOutCubic(t),
                EasingType.EaseInQuart => EaseInQuart(t),
                EasingType.EaseOutQuart => EaseOutQuart(t),
                EasingType.EaseInOutQuart => EaseInOutQuart(t),
                EasingType.EaseInSine => EaseInSine(t),
                EasingType.EaseOutSine => EaseOutSine(t),
                EasingType.EaseInOutSine => EaseInOutSine(t),
                EasingType.EaseInExpo => EaseInExpo(t),
                EasingType.EaseOutExpo => EaseOutExpo(t),
                EasingType.EaseInOutExpo => EaseInOutExpo(t),
                EasingType.EaseInCirc => EaseInCirc(t),
                EasingType.EaseOutCirc => EaseOutCirc(t),
                EasingType.EaseInOutCirc => EaseInOutCirc(t),
                EasingType.EaseInElastic => EaseInElastic(t),
                EasingType.EaseOutElastic => EaseOutElastic(t),
                EasingType.EaseInOutElastic => EaseInOutElastic(t),
                EasingType.EaseInBack => EaseInBack(t),
                EasingType.EaseOutBack => EaseOutBack(t),
                EasingType.EaseInOutBack => EaseInOutBack(t),
                EasingType.EaseInBounce => EaseInBounce(t),
                EasingType.EaseOutBounce => EaseOutBounce(t),
                EasingType.EaseInOutBounce => EaseInOutBounce(t),
                _ => LinearFunc(t)
            };
        }
        
        // 预定义常用缓动函数
        public static EasingFunction EaseInDefault { get; } = new(EasingType.EaseInQuad);
        public static EasingFunction EaseOutDefault { get; } = new(EasingType.EaseOutQuad);
        public static EasingFunction EaseInOutDefault { get; } = new(EasingType.EaseInOutQuad);
        
        #region 缓动函数实现
        
        public static double LinearFunc(double t) => t;
        
        private static double EaseInQuad(double t) => t * t;
        private static double EaseOutQuad(double t) => 1 - (1 - t) * (1 - t);
        private static double EaseInOutQuad(double t) => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        
        private static double EaseInCubic(double t) => t * t * t;
        private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
        private static double EaseInOutCubic(double t) => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        
        private static double EaseInQuart(double t) => t * t * t * t;
        private static double EaseOutQuart(double t) => 1 - Math.Pow(1 - t, 4);
        private static double EaseInOutQuart(double t) => t < 0.5 ? 8 * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 4) / 2;
        
        private static double EaseInSine(double t) => 1 - Math.Cos((t * Math.PI) / 2);
        private static double EaseOutSine(double t) => Math.Sin((t * Math.PI) / 2);
        private static double EaseInOutSine(double t) => -(Math.Cos(Math.PI * t) - 1) / 2;
        
        private static double EaseInExpo(double t) => t == 0 ? 0 : Math.Pow(2, 10 * t - 10);
        private static double EaseOutExpo(double t) => t == 1 ? 1 : 1 - Math.Pow(2, -10 * t);
        private static double EaseInOutExpo(double t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2 : (2 - Math.Pow(2, -20 * t + 10)) / 2;
        }
        
        private static double EaseInCirc(double t) => 1 - Math.Sqrt(1 - t * t);
        private static double EaseOutCirc(double t) => Math.Sqrt(1 - Math.Pow(t - 1, 2));
        private static double EaseInOutCirc(double t) => t < 0.5
            ? (1 - Math.Sqrt(1 - Math.Pow(2 * t, 2))) / 2
            : (Math.Sqrt(1 - Math.Pow(-2 * t + 2, 2)) + 1) / 2;
        
        private static double EaseInElastic(double t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return -Math.Pow(2, 10 * t - 10) * Math.Sin((t * 10 - 10.75) * ((2 * Math.PI) / 3));
        }
        
        private static double EaseOutElastic(double t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * ((2 * Math.PI) / 3)) + 1;
        }
        
        private static double EaseInOutElastic(double t)
        {
            if (t == 0) return 0;
            if (t == 1) return 1;
            return t < 0.5
                ? -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * ((2 * Math.PI) / 4.5))) / 2
                : (Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * ((2 * Math.PI) / 4.5))) / 2 + 1;
        }
        
        private static double EaseInBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            return c3 * t * t * t - c1 * t * t;
        }
        
        private static double EaseOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;
            return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
        }
        
        private static double EaseInOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c2 = c1 * 1.525;
            return t < 0.5
                ? (Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
                : (Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }
        
        private static double EaseInBounce(double t) => 1 - EaseOutBounce(1 - t);
        
        private static double EaseOutBounce(double t)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;
            
            if (t < 1 / d1)
                return n1 * t * t;
            else if (t < 2 / d1)
                return n1 * (t -= 1.5 / d1) * t + 0.75;
            else if (t < 2.5 / d1)
                return n1 * (t -= 2.25 / d1) * t + 0.9375;
            else
                return n1 * (t -= 2.625 / d1) * t + 0.984375;
        }
        
        private static double EaseInOutBounce(double t) => t < 0.5
            ? (1 - EaseOutBounce(1 - 2 * t)) / 2
            : (1 + EaseOutBounce(2 * t - 1)) / 2;
        
        #endregion
        
        // 隐式转换
        public static implicit operator EasingFunction(EasingType type) => new(type);
    }
}
