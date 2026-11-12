@echo off
REM Doble clic para arrancar el sistema en Windows. Pasa las opciones a iniciar.ps1 (ej: iniciar.cmd -Dev).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0iniciar.ps1" %*
if errorlevel 1 pause
