@echo off
setlocal
set ROOT=%~dp0

REM Backend
start "Backend" cmd /k "cd /d "%ROOT%src\EcommerceLaptop.API" && dotnet run"

REM Frontend
start "Frontend" cmd /k "cd /d "%ROOT%ecommerce-laptop-frontend" && pnpm dev"

exit /b 0


