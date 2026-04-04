namespace Eclipse.Core;

/// <summary>
/// EclipseUI 异常基类
/// </summary>
public class EclipseException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public EclipseErrorCode ErrorCode { get; }

    /// <summary>
    /// 创建 EclipseUI 异常
    /// </summary>
    public EclipseException(EclipseErrorCode errorCode, string message) 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 创建 EclipseUI 异常（带内部异常）
    /// </summary>
    public EclipseException(EclipseErrorCode errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// 创建格式化的异常消息
    /// </summary>
    public override string ToString()
    {
        return $"[{ErrorCode}] {Message}";
    }
}

/// <summary>
/// EclipseUI 错误代码
/// </summary>
public enum EclipseErrorCode
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 组件错误
    /// </summary>
    Component = 1000,

    /// <summary>
    /// 组件未找到
    /// </summary>
    ComponentNotFound = 1001,

    /// <summary>
    /// 组件创建失败
    /// </summary>
    ComponentCreationFailed = 1002,

    /// <summary>
    /// 组件 ID 无效
    /// </summary>
    InvalidComponentId = 1003,

    /// <summary>
    /// 渲染错误
    /// </summary>
    Render = 2000,

    /// <summary>
    /// 渲染上下文无效
    /// </summary>
    InvalidRenderContext = 2001,

    /// <summary>
    /// 渲染目标创建失败
    /// </summary>
    RenderTargetCreationFailed = 2002,

    /// <summary>
    /// 后端错误
    /// </summary>
    Backend = 3000,

    /// <summary>
    /// 后端初始化失败
    /// </summary>
    BackendInitializationFailed = 3001,

    /// <summary>
    /// 后端不支持
    /// </summary>
    BackendNotSupported = 3002,

    /// <summary>
    /// EGL 错误
    /// </summary>
    Egl = 3100,

    /// <summary>
    /// OpenGL 错误
    /// </summary>
    OpenGL = 3200,

    /// <summary>
    /// 生成器错误
    /// </summary>
    Generator = 4000,

    /// <summary>
    /// EUI 语法错误
    /// </summary>
    EuiSyntaxError = 4001,

    /// <summary>
    /// EUI 文件未找到
    /// </summary>
    EuiFileNotFound = 4002,

    /// <summary>
    /// 平台错误
    /// </summary>
    Platform = 5000,

    /// <summary>
    /// 窗口创建失败
    /// </summary>
    WindowCreationFailed = 5001,

    /// <summary>
    /// 平台不支持
    /// </summary>
    PlatformNotSupported = 5002,
}

/// <summary>
/// 异常工厂类
/// </summary>
public static class EclipseErrors
{
    /// <summary>
    /// 创建组件异常
    /// </summary>
    public static EclipseException ComponentNotFound(string componentName)
        => new(EclipseErrorCode.ComponentNotFound, $"Component '{componentName}' not found.");

    /// <summary>
    /// 创建无效组件 ID 异常
    /// </summary>
    public static EclipseException InvalidComponentId()
        => new(EclipseErrorCode.InvalidComponentId, "Component ID cannot be default.");

    /// <summary>
    /// 创建渲染目标异常
    /// </summary>
    public static EclipseException RenderTargetCreationFailed(string reason)
        => new(EclipseErrorCode.RenderTargetCreationFailed, $"Failed to create render target: {reason}");

    /// <summary>
    /// 创建后端初始化异常
    /// </summary>
    public static EclipseException BackendInitializationFailed(string backendName, string reason)
        => new(EclipseErrorCode.BackendInitializationFailed, $"Failed to initialize {backendName} backend: {reason}");

    /// <summary>
    /// 创建窗口创建异常
    /// </summary>
    public static EclipseException WindowCreationFailed(int errorCode)
        => new(EclipseErrorCode.WindowCreationFailed, $"Failed to create window. Error code: 0x{errorCode:X8}");

    /// <summary>
    /// 创建 EGL 异常
    /// </summary>
    public static EclipseException EglError(string operation, int errorCode)
        => new(EclipseErrorCode.Egl, $"EGL error in {operation}: 0x{errorCode:X}");

    /// <summary>
    /// 创建 OpenGL 异常
    /// </summary>
    public static EclipseException OpenGlError(string operation)
        => new(EclipseErrorCode.OpenGL, $"OpenGL error in {operation}");
}