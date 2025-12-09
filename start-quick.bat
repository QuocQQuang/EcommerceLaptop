@echo off
REM Khi ng nhanh 1-click - gi PowerShell script ci t v chy dch v
setlocal
set SCRIPT_DIR=%~dp0

echo == Cua Hang Laptop - Khoi dong nhanh==
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%start-quick.ps1"

echo.
echo Hoan tat. Neu Docker chua chay, vui long mo Docker Desktop va chay lai.
pause