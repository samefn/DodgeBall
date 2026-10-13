@echo off
:: Permite el juego Dodgeball en el Firewall de Windows (ejecutar en CADA PC que juegue).
:: Copia este archivo a la carpeta del build (junto a MiniGames.exe) y haz doble clic.
:: Pide permisos de administrador porque modifica reglas del firewall.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Solicitando permisos de administrador...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo === Quitando reglas que BLOQUEAN MiniGames.exe (se crean al cancelar el aviso del firewall) ===
powershell -NoProfile -Command "Get-NetFirewallApplicationFilter | Where-Object { $_.Program -like '*MiniGames.exe' } | Get-NetFirewallRule | Where-Object { $_.Action -eq 'Block' } | Remove-NetFirewallRule -ErrorAction SilentlyContinue"

echo === Permitiendo los puertos del juego (redes privadas y publicas) ===
netsh advfirewall firewall delete rule name="Dodgeball - Partida TCP 5555" >nul 2>&1
netsh advfirewall firewall add rule name="Dodgeball - Partida TCP 5555" dir=in action=allow protocol=TCP localport=5555 profile=any
netsh advfirewall firewall delete rule name="Dodgeball - Salas UDP 47777" >nul 2>&1
netsh advfirewall firewall add rule name="Dodgeball - Salas UDP 47777" dir=in action=allow protocol=UDP localport=47777 profile=any

if exist "%~dp0MiniGames.exe" (
    echo === Permitiendo el ejecutable %~dp0MiniGames.exe ===
    netsh advfirewall firewall delete rule name="Dodgeball - MiniGames.exe" >nul 2>&1
    netsh advfirewall firewall add rule name="Dodgeball - MiniGames.exe" dir=in action=allow program="%~dp0MiniGames.exe" enable=yes profile=any
)

echo.
echo Listo. Ya puedes crear salas y unirte desde otros PCs de la misma red.
echo (Para quitar estas reglas: Firewall de Windows ^> Configuracion avanzada ^> Reglas de entrada ^> "Dodgeball - ...")
pause
