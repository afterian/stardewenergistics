@echo off
rem Showcase: starts Stardew with only this mod, builds a whole working base step by step in a
rem scratch building on your latest save (never saved), slowly enough to watch, and saves a
rem screenshot of each step to %LOCALAPPDATA%\StardewEnergistics\selftest\shots.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pc\selftest.ps1" -Showcase %*
start "" "%LOCALAPPDATA%\StardewEnergistics\selftest\shots"
pause
