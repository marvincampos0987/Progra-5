@echo off
echo ===================================================
echo   AUTOMATED APK BUILDER - CLASIFICACION LIGAS APP
echo ===================================================
echo.
echo [1/5] Installing project dependencies (npm install)...
call npm install --no-audit --no-fund
if %errorlevel% neq 0 (
    echo Error during npm install. Make sure Node.js is installed.
    pause
    exit /b %errorlevel%
)

echo.
echo [2/5] Building Angular production web assets...
call npm run build
if %errorlevel% neq 0 (
    echo Error during build.
    pause
    exit /b %errorlevel%
)

echo.
echo [3/5] Adding Android integration via Capacitor...
if not exist "android" (
    call npx cap add android
) else (
    echo Android folder already exists, skipping add...
)

echo.
echo [4/5] Syncing assets to native Android project...
call npx cap sync
if %errorlevel% neq 0 (
    echo Error during capacitor sync.
    pause
    exit /b %errorlevel%
)

echo.
echo [5/5] Generating Android Launcher Icons and Splash screens...
call npm run generate-icons-android
if %errorlevel% neq 0 (
    echo Warning: Could not generate launcher assets. Custom icons may not be generated.
)

echo.
echo ===================================================
echo   SUCCESS: Project compiled and synchronized!
echo   Opening Android Studio to build your APK...
echo ===================================================
echo.
echo Android Studio will open shortly.
echo To build the APK, wait for Android Studio Gradle Sync to finish,
echo then click: Build -> Build Bundle(s) / APK(s) -> Build APK(s)
echo.
call npx cap open android
pause
