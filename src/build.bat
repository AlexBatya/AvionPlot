@echo off
REM ---- Путь к проекту ----
set PROJECT_PATH=%~dp0AvionPlot.csproj

REM ---- Папка для сборки (в ../bin) ----
set BUILD_DIR=%~dp0..\bin

REM ---- Чистим старую сборку ----
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"

REM ---- Создаём папки под x64 и x86 ----
mkdir "%BUILD_DIR%\x64"
mkdir "%BUILD_DIR%\x86"

REM ---- Сборка x64 ----
echo.
echo ---- Building x64 ----
dotnet publish "%PROJECT_PATH%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  /p:PublishSingleFile=false ^
  /p:PublishDir="%BUILD_DIR%\x64\"

REM ---- Сборка x86 ----
echo.
echo ---- Building x86 ----
dotnet publish "%PROJECT_PATH%" ^
  -c Release ^
  -r win-x86 ^
  --self-contained false ^
  /p:PublishSingleFile=false ^
  /p:PublishDir="%BUILD_DIR%\x86\"

echo.
echo ---- Build complete! ----
echo x64 build is in: %BUILD_DIR%\x64
echo x86 build is in: %BUILD_DIR%\x86
pause
