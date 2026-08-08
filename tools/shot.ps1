$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$proc = Get-Process chrome -ErrorAction Stop |
    Where-Object { $_.MainWindowTitle -like '*macro-key-test*' } |
    Select-Object -First 1
if (-not $proc) { throw 'janela macro-key-test nao encontrada' }

Add-Type -Namespace W -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
'@
[W.Native]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 300

$r = $proc.MainWindowHandle | ForEach-Object {
    Add-Type -Namespace W2 -Name R -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
'@
    $rect = New-Object W2.R+RECT
    [W2.R]::GetWindowRect($_, [ref]$rect) | Out-Null
    $rect
}

$bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$out = Join-Path $PSScriptRoot 'shot.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output $out
