using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Eclipse.Controls;

/// <summary>
/// 剪贴板服务 - 提供跨平台剪贴板访问
/// </summary>
public static class ClipboardService
{
    /// <summary>
    /// 设置剪贴板文本
    /// </summary>
    public static void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        
        try
        {
            // Windows 平台实现
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetTextWindows(text);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                SetTextMacOS(text);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SetTextLinux(text);
            }
        }
        catch
        {
            // 剪贴板操作失败时静默忽略
        }
    }
    
    /// <summary>
    /// 获取剪贴板文本
    /// </summary>
    public static string? GetText()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetTextWindows();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetTextMacOS();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetTextLinux();
            }
        }
        catch
        {
            // 剪贴板操作失败时返回 null
        }
        
        return null;
    }
    
    // === Windows 实现 ===
    
    private static void SetTextWindows(string text)
    {
        // 使用命令行工具作为备选方案
        // 这样可以避免引用 Windows Forms 或 WPF
        // 注意：需要对文本进行转义处理
        var escapedText = text.Replace("'", "''").Replace("\"", "\"\"");
        
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -Command \"Set-Clipboard -Value '{escapedText}'\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        using var process = Process.Start(psi);
        process?.WaitForExit(2000);
    }
    
    private static string? GetTextWindows()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = "-NoProfile -Command \"Get-Clipboard\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            return null;
        
        var text = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2000);
        
        return text.TrimEnd('\r', '\n');
    }
    
    // === macOS 实现 ===
    
    private static void SetTextMacOS(string text)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pbcopy",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true
        };
        
        using var process = Process.Start(psi);
        if (process != null)
        {
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(2000);
        }
    }
    
    private static string? GetTextMacOS()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pbpaste",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            return null;
        
        var text = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2000);
        
        return text;
    }
    
    // === Linux 实现 ===
    
    private static void SetTextLinux(string text)
    {
        // 尝试使用 xclip
        var psi = new ProcessStartInfo
        {
            FileName = "xclip",
            Arguments = "-selection clipboard",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true
        };
        
        try
        {
            using var process = Process.Start(psi);
            if (process != null)
            {
                process.StandardInput.Write(text);
                process.StandardInput.Close();
                process.WaitForExit(2000);
            }
        }
        catch
        {
            // 如果 xclip 不可用，尝试 wl-copy (Wayland)
            var psiWl = new ProcessStartInfo
            {
                FileName = "wl-copy",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true
            };
            
            using var processWl = Process.Start(psiWl);
            if (processWl != null)
            {
                processWl.StandardInput.Write(text);
                processWl.StandardInput.Close();
                processWl.WaitForExit(2000);
            }
        }
    }
    
    private static string? GetTextLinux()
    {
        // 尝试使用 xclip
        var psi = new ProcessStartInfo
        {
            FileName = "xclip",
            Arguments = "-selection clipboard -o",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        
        try
        {
            using var process = Process.Start(psi);
            if (process != null)
            {
                var text = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);
                return text;
            }
        }
        catch
        {
            // 如果 xclip 不可用，尝试 wl-paste (Wayland)
            var psiWl = new ProcessStartInfo
            {
                FileName = "wl-paste",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            
            using var processWl = Process.Start(psiWl);
            if (processWl != null)
            {
                var text = processWl.StandardOutput.ReadToEnd();
                processWl.WaitForExit(2000);
                return text;
            }
        }
        
        return null;
    }
}