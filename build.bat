@echo off
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true
cd kohya-ss_workspace_WPFver/bin/Release
explorer .
pause