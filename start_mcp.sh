#!/bin/bash

# Unity MCP Generator 启动脚本
echo "🚀 Unity MCP Generator"
echo "======================"

# 检查Node.js是否安装
if ! command -v node &> /dev/null; then
    echo "❌ Error: Node.js is not installed"
    echo "Please install Node.js from https://nodejs.org/"
    exit 1
fi

# 检查npm是否可用
if ! command -v npm &> /dev/null; then
    echo "❌ Error: npm is not available"
    exit 1
fi

echo "✅ Node.js version: $(node --version)"
echo "✅ npm version: $(npm --version)"

# 检查package.json是否存在
if [ ! -f "package.json" ]; then
    echo "❌ Error: package.json not found"
    echo "Please run this script from the project root directory"
    exit 1
fi

# 安装依赖（如果node_modules不存在）
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
    if [ $? -ne 0 ]; then
        echo "❌ Error: Failed to install dependencies"
        exit 1
    fi
    echo "✅ Dependencies installed successfully"
else
    echo "✅ Dependencies already installed"
fi

# 运行测试（可选）
echo ""
read -p "🧪 Run tests before starting server? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🧪 Running tests..."
    node test_mcp.js
    if [ $? -ne 0 ]; then
        echo "❌ Tests failed"
        exit 1
    fi
    echo "✅ Tests passed!"
fi

echo ""
echo "🚀 Starting Unity MCP Server..."
echo "📝 Server will start on stdio (for Claude Desktop integration)"
echo "❌ Press Ctrl+C to stop the server"
echo ""

# 启动MCP服务器
npm start