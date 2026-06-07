using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

namespace @_.Services;

/// <summary>
/// Detects the currently foregrounded "game" by combining two heuristics:
/// (1) the foreground window covers the whole monitor (exclusive/borderless fullscreen),
/// (2) the foreground process name is on a user-maintained list.
/// Raises <see cref="GameDetected"/> when a game becomes foreground and
/// <see cref="GameStopped"/> when it is no longer foreground.
/// </summary>
public class GameDetectionService : IDisposable
{
    public event Action<ProcessInfo>? GameDetected;
    public event Action<ProcessInfo>? GameStopped;

    private readonly Timer _timer;
    private ProcessInfo? _currentGame;
    private HashSet<string> _gameNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When false, detection is paused and no events are raised.</summary>
    public bool IsEnabled { get; set; }

    // Processes that can legitimately own a fullscreen foreground window but are not games.
    private static readonly HashSet<string> NonGameProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "ApplicationFrameHost", "ShellExperienceHost", "SearchHost",
        "StartMenuExperienceHost", "TextInputHost", "SystemSettings", "dwm",
        "LockApp", "WinStore.App", "Taskmgr"
    };

    public GameDetectionService()
    {
        _timer = new Timer(2000);
        _timer.Elapsed += Check;
        _timer.Start();
    }

    /// <summary>Replace the explicit game-name allow list (names without .exe).</summary>
    public void SetGameNames(IEnumerable<string> names)
    {
        _gameNames = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    private void Check(object? sender, ElapsedEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            var game = DetectForegroundGame();
            if (game != null)
            {
                if (_currentGame == null || _currentGame.Value.ProcessId != game.Value.ProcessId)
                {
                    _currentGame = game;
                    Console.WriteLine($"[GameDetectionService] Game detected: {game.Value.ProcessName}({game.Value.ProcessId})");
                    GameDetected?.Invoke(game.Value);
                }
            }
            else if (_currentGame != null)
            {
                var stopped = _currentGame.Value;
                _currentGame = null;
                Console.WriteLine($"[GameDetectionService] Game no longer foreground: {stopped.ProcessName}");
                GameStopped?.Invoke(stopped);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameDetectionService] Detection error: {ex.Message}");
        }
    }

    private ProcessInfo? DetectForegroundGame()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return null;
        }

        string name;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            name = process.ProcessName;
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrEmpty(name) || NonGameProcesses.Contains(name))
        {
            return null;
        }

        // Allow-list match wins regardless of window state.
        if (_gameNames.Contains(name))
        {
            return new ProcessInfo((int)pid, name);
        }

        // Otherwise treat a fullscreen foreground window as a game.
        if (IsFullscreen(hwnd))
        {
            return new ProcessInfo((int)pid, name);
        }

        return null;
    }

    private static bool IsFullscreen(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref mi))
        {
            return false;
        }

        // Window covers (or exceeds) the entire monitor bounds.
        return rect.Left <= mi.rcMonitor.Left &&
               rect.Top <= mi.rcMonitor.Top &&
               rect.Right >= mi.rcMonitor.Right &&
               rect.Bottom >= mi.rcMonitor.Bottom;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        GameDetected = null;
        GameStopped = null;
    }

    #region Win32

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    #endregion
}
