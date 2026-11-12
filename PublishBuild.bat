@echo off


call :colored "Starting build!" Blue

dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true


call :colored "Build is succeful, in kohya-ss_workspace_WPFver\bin\Release\net10.0-windows\kohya-ss_workspace_WPFver.exe" Green
cd kohya-ss_workspace_WPFver\bin\Release\net10.0-windows
explorer .
pause >nul
exit



:colored
%Windir%\System32\WindowsPowerShell\v1.0\Powershell.exe write-host -foregroundcolor %2 %1