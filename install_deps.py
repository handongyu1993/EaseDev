#!/usr/bin/env python3
"""
Install dependencies for Unity MCP Server
"""

import subprocess
import sys
import os

def install_package(package):
    """Install a Python package using pip"""
    try:
        subprocess.check_call([sys.executable, "-m", "pip", "install", package])
        print(f"✅ Successfully installed {package}")
        return True
    except subprocess.CalledProcessError:
        print(f"❌ Failed to install {package}")
        return False

def main():
    print("🚀 Unity MCP Server - Installing Dependencies")
    print("=" * 50)

    # Required packages
    packages = [
        "websockets",
        "asyncio",
    ]

    # Optional MCP package (if available)
    optional_packages = [
        "mcp",
        "model-context-protocol"
    ]

    # Install required packages
    success_count = 0
    for package in packages:
        if install_package(package):
            success_count += 1

    # Try to install optional packages
    for package in optional_packages:
        print(f"\n📦 Trying to install optional package: {package}")
        install_package(package)  # Don't count failures for optional packages

    print(f"\n📊 Installation Summary:")
    print(f"Required packages: {success_count}/{len(packages)} installed")

    if success_count == len(packages):
        print("✅ All required dependencies installed successfully!")
        print("\n🎯 Next steps:")
        print("1. Open Unity Editor")
        print("2. Go to Tools → Unity MCP → Simple Bridge")
        print("3. Click 'Start Server' in the Unity window")
        print("4. Run: python3 unity_mcp_server.py --test")
        return True
    else:
        print("❌ Some required dependencies failed to install")
        print("Please install them manually:")
        for package in packages:
            print(f"  pip install {package}")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)