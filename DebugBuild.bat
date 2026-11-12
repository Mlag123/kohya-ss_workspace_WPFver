@echo off


call :colored "Starting build!" Blue

dotnet restore
dotnet build -c Debug

pause >nul
exit



:colored
%Windir%\System32\WindowsPowerShell\v1.0\Powershell.exe write-host -foregroundcolor %2 %1