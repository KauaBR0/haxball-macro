param([string]$vkHex = '0x78', [int]$holdMs = 800)
$ErrorActionPreference = 'Stop'
Add-Type -Namespace W -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public System.IntPtr dwExtraInfo; }

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public System.IntPtr dwExtraInfo; }

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
public struct InputUnion
{
    [System.Runtime.InteropServices.FieldOffset(0)] public MOUSEINPUT mi;
    [System.Runtime.InteropServices.FieldOffset(0)] public KEYBDINPUT ki;
    [System.Runtime.InteropServices.FieldOffset(0)] public HARDWAREINPUT hi;
}

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct INPUT { public uint type; public InputUnion u; }

[System.Runtime.InteropServices.DllImport("user32.dll")]
public static extern uint SendInput(uint n, INPUT[] p, int cb);

public static uint SendHold(ushort vk, int holdMs)
{
    int cb = System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT));
    var inputs = new INPUT[1];
    inputs[0].type = 1; // INPUT_KEYBOARD
    inputs[0].u.ki.wVk = vk;
    uint r1 = SendInput(1, inputs, cb);
    System.Threading.Thread.Sleep(holdMs);
    inputs[0].u.ki.dwFlags = 2; // KEYEVENTF_KEYUP
    uint r2 = SendInput(1, inputs, cb);
    return r1 + r2;
}
'@

$vk = [Convert]::ToUInt16($vkHex, 16)
$r = [W.Native]::SendHold($vk, $holdMs)
Write-Output "hold vk=0x$($vk.ToString('X')) por $holdMs ms (SendInput retornou $r/2)"
