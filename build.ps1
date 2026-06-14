# --- CONFIGURATION ---
$gamePath = "C:\PATH\TO\Death Pong Demo"
$managed = "$gamePath\Death Pong Demo_Data\Managed"
$bepPath = "$gamePath\BepInEx\core"
$output = "$gamePath\BepInEx\plugins\BespokeTrainer.dll"

if (!(Test-Path "$bepPath\BepInEx.dll")) { $bepPath = "$gamePath" }

$gameProcess = Get-Process -Name "Death Pong Demo" -ErrorAction SilentlyContinue
if ($gameProcess) {
    Write-Host "WARNING: Game is currently running!" -ForegroundColor Yellow
    Write-Host "Please close the game and then press any key to continue building..." -ForegroundColor White
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

$libs = @(
    "$managed\mscorlib.dll",
    "$managed\netstandard.dll",
    "$managed\System.dll",
    "$managed\System.Core.dll",
    "$managed\UnityEngine.dll",
    "$managed\UnityEngine.CoreModule.dll",
    "$managed\UnityEngine.IMGUIModule.dll",
    "$managed\UnityEngine.PhysicsModule.dll",
    "$managed\UnityEngine.InputLegacyModule.dll",
    "$managed\UnityEngine.VideoModule.dll",
    "$managed\UnityEngine.AnimationModule.dll",
    "$managed\UnityEngine.UIModule.dll",
    "$managed\UnityEngine.UI.dll",
    "$managed\Unity.TextMeshPro.dll",
    "$managed\Assembly-CSharp.dll",
    "$bepPath\BepInEx.dll",
    "$bepPath\0Harmony.dll"
)

foreach ($lib in $libs) {
    if (!(Test-Path $lib)) {
        Write-Host "MISSING DLL: $lib" -ForegroundColor Red
        pause
        exit
    }
}

$cscArgs = @("/target:library", "/nostdlib", "/noconfig", "/nowin32manifest", "/out:$output")
foreach ($lib in $libs) { $cscArgs += "-r:$lib" }
$cscArgs += "Trainer.cs"

Write-Host "Assembling trainer with UI and TMPro support..." -ForegroundColor Cyan
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

& $csc $cscArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS! Trainer.dll updated." -ForegroundColor Green
} else {
    Write-Host "`nBUILD FAILED." -ForegroundColor Red
}
pause
