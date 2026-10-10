@echo off
REM ===================================================================
REM  Teknologi Game - bantu jalankan Unity lewat command line
REM  Pakai:  jalankan_unity.bat build | verify | screenshot
REM  (tanpa argumen = build)
REM ===================================================================

set UNITY="C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe"
REM Kalau Unity dipasang di versi lain, set variabel UNITY lewat environment:
REM   set UNITY="C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
if not exist %UNITY% (
    if exist "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" (
        set UNITY="C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
    )
)

REM Project path dihitung dari lokasi file ini, jadi tidak terikat folder pengguna.
set PROJ=%~dp0..\UTS_TeknologiGame
set LOGDIR=%~dp0

if /I "%1"=="verify"     goto verify
if /I "%1"=="screenshot" goto screenshot
goto build

:build
echo === Membuat scene Arena + BAKE NavMesh ===
%UNITY% -batchmode -accept-apiupdate -projectPath "%PROJ%" -executeMethod SceneBuilder.BuildAll -logFile "%LOGDIR%build.log" -quit
echo EXITCODE=%ERRORLEVEL%
goto done

:verify
echo === Verifikasi Play Mode (26 pemeriksaan) ===
%UNITY% -batchmode -accept-apiupdate -projectPath "%PROJ%" -executeMethod PlayModeCheck.RunAll -logFile "%LOGDIR%verify.log"
echo EXITCODE=%ERRORLEVEL%
goto done

:screenshot
echo === Ambil screenshot sudut arena ===
%UNITY% -batchmode -accept-apiupdate -projectPath "%PROJ%" -executeMethod ScreenshotBukti.Ambil -logFile "%LOGDIR%screenshot.log"
echo EXITCODE=%ERRORLEVEL%
goto done

:done
echo.
echo Log ada di: %LOGDIR%
pause
