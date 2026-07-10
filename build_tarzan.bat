@echo off
setlocal enabledelayedexpansion

set APPNAME=TarzanRungXanhCoop
set OUTEXE=%APPNAME%.exe

set VBC=
for %%v in (v4.0.30319 v4.0.30128 v4.0.21006 v4.0.20506) do (
    if exist "%WINDIR%\Microsoft.NET\Framework\%%v\vbc.exe" (
        set "VBC=%WINDIR%\Microsoft.NET\Framework\%%v\vbc.exe"
    )
)
if "%VBC%"=="" (
    if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe" (
        set "VBC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe"
    )
)

if "%VBC%"=="" (
    echo [LOI] Khong tim thay vbc.exe cho .NET Framework 4.x
    echo Kiem tra thu muc %WINDIR%\Microsoft.NET\Framework\ hoac Framework64\
    pause
    exit /b 1
)

echo Dung trinh bien dich: %VBC%
echo Dang build %OUTEXE% ...
echo.

"%VBC%" /nologo /target:winexe /out:%OUTEXE% /optimize+ /optionstrict+ /optionexplicit+ ^
    /reference:System.dll,System.Windows.Forms.dll,System.Drawing.dll ^
    Program.vb Form1.vb TarzanGame.vb NetworkPeer.vb

if errorlevel 1 (
    echo.
    echo [LOI] Build that bai.
    pause
    exit /b 1
) else (
    echo.
    echo [OK] Build thanh cong: %OUTEXE%
    echo [LUU Y] Nho co thu muc "Assets" cung thu muc voi %OUTEXE%. Cac file PNG
    echo          duoc tai su dung dung ten cu (player0.png, enemy_soldier.png,
    echo          enemy_shelled.png, enemy_shell.png, enemy_boss.png, tile_ground.png,
    echo          tile_pipe.png, tile_questionblock.png, bullet_player.png,
    echo          bullet_enemy.png, powerup_weapon.png, powerup_life.png,
    echo          background.png). Neu thieu file nao, game se tu dong fallback
    echo          ve bang GDI+ hinh hoc (rieng day leo luon ve bang GDI+, khong
    echo          can sprite).
)

pause
