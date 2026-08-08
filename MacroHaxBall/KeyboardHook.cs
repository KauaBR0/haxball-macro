using System.ComponentModel;
using System.Runtime.InteropServices;
using static MacroHaxBall.NativeMethods;

namespace MacroHaxBall;

/// <summary>
/// Hook global de teclado (WH_KEYBOARD_LL).
/// Instalado em thread dedicada com message pump (GetMessage/DispatchMessage) —
/// obrigatório para o Windows entregar os eventos ao thread que instalou o hook.
/// O callback apenas despacha o evento; trabalho pesado (sleeps, SendInput)
/// deve ficar fora daqui (o hook tem timeout e pode ser removido pelo sistema).
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private readonly LowLevelKeyboardProc _proc;           // campo forte: impede GC do delegate
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private IntPtr _hookId;
    private volatile int _threadId;
    private int _installError;

    /// <summary>Disparado no thread do hook. Seja rápido: não bloqueie aqui.</summary>
    public event Action<KeyEventInfo>? KeyEvent;

    public KeyboardHook() => _proc = HookCallback;

    public void Start()
    {
        _thread = new Thread(Run) { Name = "kb-hook", IsBackground = false };
        _thread.Start();
        _ready.Wait();
        if (_hookId == IntPtr.Zero)
            throw new Win32Exception(_installError, "SetWindowsHookEx(WH_KEYBOARD_LL) falhou");
    }

    public void Stop()
    {
        int tid = _threadId;
        if (tid != 0)
            PostThreadMessage(tid, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread?.Join(TimeSpan.FromSeconds(2));
    }

    public void Dispose() => Stop();

    private void Run()
    {
        _threadId = GetCurrentThreadId();
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hookId == IntPtr.Zero)
            _installError = Marshal.GetLastWin32Error();
        _ready.Set();
        if (_hookId == IntPtr.Zero)
            return;

        var msg = new MSG();
        while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            bool down = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
            bool up = msg is WM_KEYUP or WM_SYSKEYUP;

            if (down || up)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var info = new KeyEventInfo
                {
                    Vk = (Vk)(data.vkCode & 0xFFFF),
                    ScanCode = data.scanCode,
                    IsDown = down,
                    IsUp = up,
                    IsInjected = (data.flags & LLKHF_INJECTED) != 0,
                };

                KeyEvent?.Invoke(info);

                if (info.Consume)
                    return (IntPtr)1; // bloqueia: não repassa aos demais hooks/app
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
