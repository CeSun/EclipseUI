using System;

namespace Eclipse.Core.Animation
{
    /// <summary>
    /// 动画基类，提供通用动画功能
    /// </summary>
    public abstract class AnimationBase : IAnimation
    {
        protected double _elapsedTime;
        protected bool _isPlaying;
        protected bool _isCompleted;
        protected bool _isReversed;
        
        /// <summary>
        /// 动画时长（秒）
        /// </summary>
        public double Duration { get; set; }
        
        /// <summary>
        /// 动画是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 动画是否已完成
        /// </summary>
        public bool IsCompleted => _isCompleted;
        
        /// <summary>
        /// 当前进度 (0-1)
        /// </summary>
        public double Progress => Duration > 0 ? Math.Clamp(_elapsedTime / Duration, 0, 1) : 0;
        
        /// <summary>
        /// 缓动函数
        /// </summary>
        public EasingFunction Easing { get; set; } = new EasingFunction(EasingType.Linear);
        
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsLooping { get; set; }
        
        /// <summary>
        /// 是否来回播放（ping-pong）
        /// </summary>
        public bool IsPingPong { get; set; }
        
        /// <summary>
        /// 动画完成事件
        /// </summary>
        public event EventHandler<AnimationCompletedEventArgs>? Completed;
        
        /// <summary>
        /// 动画更新事件（每帧触发，参数为缓动后的进度值）
        /// </summary>
        public event EventHandler<double>? Updated;
        
        protected AnimationBase(double duration = 1.0)
        {
            Duration = duration;
        }
        
        public virtual void Play()
        {
            _isPlaying = true;
            _isCompleted = false;
        }
        
        public virtual void Pause()
        {
            _isPlaying = false;
        }
        
        public virtual void Stop()
        {
            _isPlaying = false;
            _isCompleted = false;
            _elapsedTime = 0;
            _isReversed = false;
        }
        
        public virtual void Reset()
        {
            _elapsedTime = 0;
            _isCompleted = false;
            _isReversed = false;
        }
        
        public virtual void Update(double deltaTime)
        {
            if (!_isPlaying || _isCompleted)
                return;
            
            // 更新时间
            if (_isReversed)
                _elapsedTime -= deltaTime;
            else
                _elapsedTime += deltaTime;
            
            // 检查完成
            if (_elapsedTime >= Duration)
            {
                if (IsLooping || IsPingPong)
                {
                    if (IsPingPong)
                    {
                        _isReversed = !_isReversed;
                        _elapsedTime = Duration;
                    }
                    else
                    {
                        _elapsedTime = 0;
                    }
                }
                else
                {
                    _elapsedTime = Duration;
                    _isCompleted = true;
                    _isPlaying = false;
                    OnCompleted();
                }
            }
            else if (_elapsedTime <= 0 && _isReversed)
            {
                if (IsLooping)
                {
                    _isReversed = false;
                    _elapsedTime = 0;
                }
                else
                {
                    _elapsedTime = 0;
                    _isCompleted = true;
                    _isPlaying = false;
                    OnCompleted();
                }
            }
            
            // 触发更新事件
            OnUpdated();
        }
        
        /// <summary>
        /// 获取缓动后的进度值
        /// </summary>
        protected double GetEasedProgress()
        {
            return Easing.Apply(Progress);
        }
        
        protected virtual void OnCompleted()
        {
            Completed?.Invoke(this, new AnimationCompletedEventArgs(this));
        }
        
        protected virtual void OnUpdated()
        {
            Updated?.Invoke(this, GetEasedProgress());
        }
    }
}
