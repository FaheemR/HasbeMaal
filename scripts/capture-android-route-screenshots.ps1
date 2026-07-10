param(
    [string]$PackageName = "io.github.faheemr.hasbemaal",
    [string]$ActivityName = "crc646a6a0afffa9c488e.MainActivity",
    [string]$OutputDirectory = "artifacts/android-screenshots"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$routes = @(
    @{ Name = "dashboard"; Tap = "108 2296" },
    @{ Name = "transactions"; Tap = "324 2296" },
    @{ Name = "add"; Tap = "540 2296" },
    @{ Name = "planning"; Tap = "756 2296" },
    @{ Name = "settings"; Tap = "972 2296" }
)

adb shell am start -W "$PackageName/$ActivityName" | Out-Null

foreach ($route in $routes) {
    if ($route.Tap) {
        adb shell input tap $route.Tap
    }

    $devicePath = "/sdcard/hasbemaal-$($route.Name).png"
    $hierarchyPath = "/sdcard/hasbemaal-$($route.Name).xml"
    $localPath = Join-Path $OutputDirectory "$($route.Name).png"
    adb shell uiautomator dump $hierarchyPath | Out-Null
    adb shell screencap -p $devicePath
    adb pull $devicePath $localPath | Out-Null
    Write-Host "Captured $localPath"
}