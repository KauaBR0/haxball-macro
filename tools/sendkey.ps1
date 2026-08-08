$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$key = $args[0]
$proc = Get-Process chrome -ErrorAction Stop |
    Where-Object { $_.MainWindowTitle -like '*macro-key-test*' } |
    Select-Object -First 1
if (-not $proc) { throw 'janela macro-key-test nao encontrada' }

Add-Type -Namespace W -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
'@
[W.Native]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 400

$wsh = New-Object -ComObject WScript.Shell
$wsh.SendKeys($key)
Write-Output "sent '$key'"
