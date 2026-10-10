@echo off
REM ===================================================================
REM  Teknologi Game - bantu jalankan Unity lewat command line
REM  Pakai:  jalankan_unity.bat build | verify | screenshot
REM  (tanpa argumen = build)
REM ===================================================================

set UNITY="C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe"
set PROJ=C:\Users\ACER\Documents\JHOSUA\Kuliah Jhosua\Teknologi_Game\UTS\UTS_TeknologiGame
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
echo === Verifikasi Play Mode (24 pemeriksaan) ===
%UNITY% -batchmode -accept-apiupdate -projectPath "%PROJ%" -executeMethod PlayModeCheck.RunAll -logFile "%LOGDIR%verify.log"
echo EXITCODE=%ERRORLEVEL%
goto done

:screenshot
echo === Ambil screenshot bukti ===
%UNITY% -batchmode -accept-apiupdate -projectPath "%PROJ%" -executeMethod ScreenshotBukti.Ambil -logFile "%LOGDIR%screenshot.log"
echo EXITCODE=%ERRORLEVEL%
goto done

:done
echo.
echo Log ada di: %LOGDIR%
pause
