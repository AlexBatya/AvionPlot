@echo off
REM ---- Путь к проекту ----
set PROJECT_PATH=%~dp0AvionPlot.csproj

REM ---- Папка для сборки ----
set BUILD_DIR=%~dp0build

REM ---- Чистим старую сборку ----
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"

REM ---- Собираем релиз single-file exe ----
dotnet publish "%PROJECT_PATH%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:PublishDir="%BUILD_DIR%\"

echo.
echo ---- Build complete! ----
echo Single exe is in: %BUILD_DIR%
pause
