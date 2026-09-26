@echo off
rem Avvia MACEMU nel browser (Blazor WebAssembly) su http://localhost:5200
cd /d "%~dp0"
set ASPNETCORE_URLS=http://localhost:5200
start "" http://localhost:5200
dotnet run --project MACEMU.NET\MacEmu.Web
pause
