@echo off
setlocal

rem Runs the BoardMaster.Wpf client. Works regardless of the current directory
rem since it always cd's to this file's own location (the repo root) first.
cd /d "%~dp0"

echo Starting BoardMaster client...
dotnet run --project "src\BoardMaster.Wpf" -c Release

if errorlevel 1 (
    echo.
    echo Something went wrong. See the output above.
    pause
)

endlocal
