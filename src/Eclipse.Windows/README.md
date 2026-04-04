# Eclipse.Windows

EclipseUI Windows 平台支持。

## 渲染后端

| 后端 | 类 | 说明 |
|------|-----|------|
| **ANGLE** | `AngleD3D11Context` | D3D11 后端 (推荐) |
| **OpenGL** | `WglContext` | 原生 WGL |
| **CPU** | - | 软件渲染 (后备) |

## 窗口系统

使用纯 Win32 API 实现，无 WinForms/WPF 依赖：

- `WindowImpl` - Win32 窗口封装
- `NativeMethods` - Win32 API 声明
- `Application` - 应用程序入口

## 用法

```csharp
// 最简单的方式
Eclipse.Windows.Application.Run<MyPage>();

// 指定渲染后端
var window = new WindowImpl(RenderBackend.Angle);
window.Content = BuildMyUI();
window.ShowDialog();

// 可用的后端
public enum RenderBackend
{
    CPU,        // 软件渲染
    OpenGL,     // 原生 OpenGL
    Angle       // ANGLE/D3D11 (推荐)
}
```

## ANGLE 集成

使用 `Avalonia.Angle.Windows.Natives` NuGet 包，自动部署 ANGLE 运行时。

### 优势

- **兼容性** - 在不支持 OpenGL 的系统上工作
- **性能** - D3D11 硬件加速
- **稳定性** - 避免 OpenGL 驱动问题