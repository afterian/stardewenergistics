@echo off
rem Build what's in this folder right now (any branch, even unsaved changes) and send it to the Decks.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-test.ps1"
pause
