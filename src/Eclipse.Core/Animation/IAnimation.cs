using System;

namespace Eclipse.Core.Animation
{
    /// <summary>
    /// 动画接口
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// 动画是否正在播放
        /// </summary>
        bool IsPlaying { get; }
        
        /// <summary>
        /// 动画是否已完成
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// 动画时长（秒）
        /// </summary>
        double Duration { get; }
        
        /// <summary>
        /// 当前进度 (0-1)
        /// </summary>
        double Progress { get; }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        void Play();
        
        /// <summary>
        /// 暂停动画
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 停止动画（重置到起始状态）
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 重置动画
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 更新动画状态
        /// </summary>
        /// <param name="deltaTime">帧间隔时间（秒）</param>
        void Update(double deltaTime);
    }
    
    /// <summary>
    /// 动画完成事件参数
    /// </summary>
    public class AnimationCompletedEventArgs : EventArgs
    {
        public IAnimation Animation { get; }
        public AnimationCompletedEventArgs(IAnimation animation) => Animation = animation;
    }
}
