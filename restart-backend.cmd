@echo off
setlocal

set "ROOT=%~dp0"
set "API_DIR=%ROOT%panApi"

REM Stop running backend if any
taskkill /F /IM PanSystem.exe >nul 2>nul

REM Start backend
pushd "%API_DIR%"
dotnet run
popd
endlocal
