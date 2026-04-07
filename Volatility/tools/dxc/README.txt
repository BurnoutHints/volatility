Place the dxc compiler here so Volatility can compile shader resources.

Supported layouts:
- tools/dxc/dxc(.exe)
- tools/dxc/<rid>/dxc(.exe) (e.g. win-x64, linux-x64, osx-arm64)
- tools/dxc/bin/dxc(.exe)

You can also set the VOLATILITY_DXC_PATH environment variable to an explicit
compiler path.
