@echo off
REM ==========================================================================
REM generate_fight.bat
REM
REM End-to-end 2D -> 3D build for creatures' battle (Fight) animations:
REM   1) every Fight-<id>-A-FF.png frame -> a rounded, smooth-shaded body OBJ
REM      (+ a flat _fx OBJ for a slash / trail glow), one shared .mtl + palette
REM      .png, a .vox copy when the model fits MagicaVoxel's 255 voxels, and
REM      Fight_<id>.json                                  (fight_to_vox.py)
REM   2) QA previews: 2D vs 3D from the battle camera and turned 35/70 deg,
REM      one GIF per animation plus a contact sheet      (fight_preview.py)
REM   3) deploy smoothed\ + the manifest into
REM      WindingTale2\Assets\Resources\Fights3D\<id>, where FightModel3D
REM      picks them up in battle (no files there = the 2D sprite as before)
REM
REM Steps 1 and 2 run creatures in parallel (one process per creature).
REM
REM Usage:
REM     generate_fight <id> [<id> ...]      e.g. generate_fight 001 002 003 701
REM     generate_fight all                  every creature under WindingTale's Fights
REM ==========================================================================

setlocal EnableDelayedExpansion

if "%~1"=="" (
    echo Usage: generate_fight ^<id^> [^<id^> ...]     or     generate_fight all
    exit /b 1
)

set "ROOT=D:\SourceCode\Git\toneyisnow\windingtale"
set "GEN=%ROOT%\Tools\Vox_Generator\fight_to_vox.py"
set "PREVIEW=%ROOT%\Tools\Vox_Generator\fight_preview.py"
set "SRC=%ROOT%\WindingTale\Assets\Resources\Fights"
set "OUT=%ROOT%\Resources\Remastered\Fights3D"
set "UNITY=%ROOT%\WindingTale2\Assets\Resources\Fights3D"

set "IDS="
if /I "%~1"=="all" (
    for /d %%D in ("%SRC%\*") do (
        set "NAME=%%~nxD"
        echo !NAME!| findstr /r /x "[0-9][0-9][0-9]" >nul
        if !errorlevel! equ 0 set "IDS=!IDS! !NAME!"
    )
) else (
    set "IDS=%*"
)

set "DIRS="
for %%I in (%IDS%) do set "DIRS=!DIRS! "%OUT%\%%I""

echo === [1/3] PNG frames -^> OBJ ^(+ VOX^)
python "%GEN%" %IDS% --out "%OUT%"
if errorlevel 1 (
    echo ERROR: fight_to_vox.py failed, see above
    exit /b 1
)

echo.
echo === [2/3] previews
python "%PREVIEW%" %DIRS%
if errorlevel 1 (
    echo ERROR: fight_preview.py failed, see above
    exit /b 1
)

echo.
echo === [3/3] copy to Unity Assets   ^( %UNITY% ^)
for %%I in (%IDS%) do (
    REM start clean, so models dropped by a re-run do not linger in the project
    if exist "%UNITY%\%%I" rmdir /s /q "%UNITY%\%%I"
    mkdir "%UNITY%\%%I"
    copy /Y "%OUT%\%%I\smoothed\*.*" "%UNITY%\%%I\" >nul
    copy /Y "%OUT%\%%I\Fight_%%I.json" "%UNITY%\%%I\" >nul
    if errorlevel 1 (
        echo ERROR: deploy failed on %%I
        exit /b 1
    )
)

echo.
echo Done. Output in %OUT%, deployed to %UNITY%
endlocal
