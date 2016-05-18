@echo off
powershell.exe -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile -File ".\build.ps1"
if %errorlevel% neq 0 exit /b %errorlevel%

powershell.exe -ExecutionPolicy Bypass -NoLogo -NonInteractive -NoProfile -File ".\test.ps1"
if %errorlevel% neq 0 exit /b %errorlevel%
