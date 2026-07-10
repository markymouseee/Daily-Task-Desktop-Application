using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DailyTasks.Services;

/// <summary>
/// Registers Ctrl+Shift+T system-wide and raises <see cref="Pressed"/> when it fires.
/// Registration is exclusive per key combination: if another app already owns it,
/// <see cref="IsRegistered"/> stays false and the app carries on without it.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x4459;

    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkT = 0x54;

    private IntPtr _hwnd;
    private HwndSource? _source;

    public event EventHandler? Pressed;

    public bool IsRegistered { get; private set; }

    public static string Gesture => "Ctrl + Shift + T";

    public void Register(IntPtr hwnd)
    {
        if (IsRegistered)
        {
            return;
        }

        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(OnWindowMessage);

        IsRegistered = RegisterHotKey(hwnd, HotkeyId, ModControl | ModShift | ModNoRepeat, VkT);
    }

    public void Dispose()
    {
        if (_source is not null)
        {
            _source.RemoveHook(OnWindowMessage);
            _source = null;
        }

        if (IsRegistered)
        {
            UnregisterHotKey(_hwnd, HotkeyId);
            IsRegistered = false;
        }
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey || wParam.ToInt32() != HotkeyId)
        {
            return IntPtr.Zero;
        }

        Pressed?.Invoke(this, EventArgs.Empty);
        handled = true;
        return IntPtr.Zero;
    }

    // DllImport rather than LibraryImport: the latter would drag AllowUnsafeBlocks
    // into the whole project for two calls.
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
