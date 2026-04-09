using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Eclipse.Core.Abstractions;

namespace Eclipse.Windows;

/// <summary>
/// Windows 平台剪贴板实现
/// </summary>
public class WindowsClipboard : IClipboard
{
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        
        try
        {
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
        catch
        {
            // 剪贴板操作失败时静默忽略
        }
    }
    
    public string? GetText()
    {
        try
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
        catch
        {
            return null;
        }
    }
}
