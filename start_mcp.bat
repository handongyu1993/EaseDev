@echo off
title Unity MCP Generator

echo 🚀 Unity MCP Generator
echo ======================

REM 检查Node.js是否安装
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Error: Node.js is not installed
    echo Please install Node.js from https://nodejs.org/
    pause
    exit /b 1
)

REM 检查npm是否可用
npm --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Error: npm is not available
    pause
    exit /b 1
)

echo ✅ Node.js version:
node --version
echo ✅ npm version:
npm --version

REM 检查package.json是否存在
if not exist "package.json" (
    echo ❌ Error: package.json not found
    echo Please run this script from the project root directory
    pause
    exit /b 1
)

REM 安装依赖（如果node_modules不存在）
if not exist "node_modules" (
    echo 📦 Installing dependencies...
    npm install
    if %errorlevel% neq 0 (
        echo ❌ Error: Failed to install dependencies
        pause
        exit /b 1
    )
    echo ✅ Dependencies installed successfully
) else (
    echo ✅ Dependencies already installed
)

REM 询问是否运行测试
echo.
set /p run_tests="🧪 Run tests before starting server? (y/n): "
if /i "%run_tests%"=="y" (
    echo 🧪 Running tests...
    node test_mcp.js
    if %errorlevel% neq 0 (
        echo ❌ Tests failed
        pause
        exit /b 1
    )
    echo ✅ Tests passed!
)

echo.
echo 🚀 Starting Unity MCP Server...
echo 📝 Server will start on stdio (for Claude Desktop integration)
echo ❌ Press Ctrl+C to stop the server
echo.

REM 启动MCP服务器
npm start

pause