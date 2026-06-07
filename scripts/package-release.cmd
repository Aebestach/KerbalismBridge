@echo off
REM Run package-release.ps1 from CMD (CMD cannot execute .ps1 directly).
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0package-release.ps1" %*
set "exitCode=%ERRORLEVEL%"
endlocal & exit /b %exitCode%
