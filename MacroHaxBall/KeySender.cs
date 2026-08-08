using System.ComponentModel;
using System.Runtime.InteropServices;
using static MacroHaxBall.NativeMethods;

namespace MacroHaxBall;

/// <summary>Sintetiza teclas via SendInput. Chamar apenas fora do callback do hook (usa Thread.Sleep).</summary>
public sealed class KeySender
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    /// <summary>Down + hold + up para uma tecla.</summary>
    public void Press(Vk key, int holdMs)
    {
        SendKey(key, keyUp: false);
        if (holdMs > 0)
            Thread.Sleep(holdMs);
        SendKey(key, keyUp: true);
    }

    /// <summary>Sequência de pressionamentos: count × (down/hold/up) com intervalo entre eles.</summary>
    public void Burst(Vk key, int count, int holdMs, int interKeyDelayMs)
    {
        for (int i = 0; i < count; i++)
        {
            Press(key, holdMs);
            if (i < count - 1 && interKeyDelayMs > 0)
                Thread.Sleep(interKeyDelayMs);
        }
    }

    public void SendKey(Vk key, bool keyUp)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = (ushort)MapVirtualKey((uint)key, MAPVK_VK_TO_VSC),
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };

        if (SendInput(1, [input], InputSize) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"SendInput falhou para {key}");
    }
}
