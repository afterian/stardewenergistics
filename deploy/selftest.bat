@echo off
rem In-game self-test: builds this folder, starts Stardew with only this mod, runs every
rem scenario on your latest save WITHOUT saving, closes the game and shows the report.
rem   selftest.bat               run it
rem   selftest.bat -Watcher on   also self-test every build the PC watcher makes (off: -Watcher off)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pc\selftest.ps1" %*
pause
