using System;
using System.Collections.Generic;

namespace Eclipse.Core.Animation
{
    /// <summary>
    /// 动画管理器 - 管理所有活动动画，由 MainLoop 驱动
    /// </summary>
    public class AnimationManager
    {
        private readonly List<IAnimation> _animations = new();
        private readonly List<IAnimation> _pendingAdd = new();
        private readonly List<IAnimation> _pendingRemove = new();
        private bool _isUpdating;
        
        /// <summary>
        /// 活动动画数量
        /// </summary>
        public int Count => _animations.Count;
        
        /// <summary>
        /// 是否有活动动画
        /// </summary>
        public bool HasActiveAnimations => _animations.Count > 0;
        
        /// <summary>
        /// 添加动画
        /// </summary>
        public T Add<T>(T animation) where T : IAnimation
        {
            if (_isUpdating)
                _pendingAdd.Add(animation);
            else
                _animations.Add(animation);
            
            return animation;
        }
        
        /// <summary>
        /// 移除动画
        /// </summary>
        public void Remove(IAnimation animation)
        {
            if (_isUpdating)
                _pendingRemove.Add(animation);
            else
                _animations.Remove(animation);
        }
        
        /// <summary>
        /// 移除所有动画
        /// </summary>
        public void Clear()
        {
            if (_isUpdating)
            {
                _pendingRemove.AddRange(_animations);
            }
            else
            {
                _animations.Clear();
            }
        }
        
        /// <summary>
        /// 更新所有动画（由 MainLoop 调用）
        /// </summary>
        public void Update(double deltaTime)
        {
            _isUpdating = true;
            
            try
            {
                // 更新所有动画
                for (int i = _animations.Count - 1; i >= 0; i--)
                {
                    var animation = _animations[i];
                    animation.Update(deltaTime);
                    
                    // 自动移除已完成的非循环动画
                    if (animation.IsCompleted)
                    {
                        _pendingRemove.Add(animation);
                    }
                }
            }
            finally
            {
                _isUpdating = false;
                
                // 处理待添加的动画
                foreach (var animation in _pendingAdd)
                {
                    _animations.Add(animation);
                }
                _pendingAdd.Clear();
                
                // 处理待移除的动画
                foreach (var animation in _pendingRemove)
                {
                    _animations.Remove(animation);
                }
                _pendingRemove.Clear();
            }
        }
    }
    
    /// <summary>
    /// 动画扩展方法
    /// </summary>
    public static class AnimationExtensions
    {
        /// <summary>
        /// 创建并播放动画
        /// </summary>
        public static T Play<T>(this T animation, AnimationManager? manager = null) where T : IAnimation
        {
            manager?.Add(animation);
            animation.Play();
            return animation;
        }
        
        /// <summary>
        /// 创建透明度动画
        /// </summary>
        public static DoubleAnimation FadeTo(this AnimationManager manager, double to, double duration = 0.3)
        {
            var animation = new DoubleAnimation(1, to, duration);
            manager.Add(animation);
            return animation;
        }
        
        /// <summary>
        /// 创建淡入动画
        /// </summary>
        public static DoubleAnimation FadeIn(this AnimationManager manager, double duration = 0.3)
        {
            return manager.FadeTo(1, duration);
        }
        
        /// <summary>
        /// 创建淡出动画
        /// </summary>
        public static DoubleAnimation FadeOut(this AnimationManager manager, double duration = 0.3)
        {
            return manager.FadeTo(0, duration);
        }
    }
}
