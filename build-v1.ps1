param()
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'logingui.csproj'
$output = Join-Path $PSScriptRoot 'bin\v1.2'

if (Test-Path $output) { Remove-Item -Recurse -Force $output }

Write-Output 'Building unified single-file logingui v1.2...'
dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $output -v quiet

# 清理 NuGet 附带的无用 xml 说明文档与空目录
Get-ChildItem -Path $output -Filter "*.xml" -Recurse | Remove-Item -Force
if (Test-Path "$output\runtimes") { Remove-Item -Recurse -Force "$output\runtimes" }

Write-Output "v1.2: Unified build is ready at $output\logingui.exe"

