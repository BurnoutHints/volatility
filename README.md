# Volatility
![Logo](volatility_logo.png)
### Burnout Paradise platform-agnostic resource interface

## Installation
### Users
Download and extract the latest version of the application from the [Releases page](https://github.com/BurnoutHints/volatility/releases/latest).

### Developers
Ensure you have the necessary prerequisites to develop .NET 7.0 applications on your machine.

Compiling the application is as simple as opening the project within Visual Studio 2022, or by running `dotnet build`.

## Commands
NOTE: This may not be entirely comprehensive. Run "help" for a full list of commands within the application.

#### PortTexture
- Ports texture data from the specified source format to the specified destination format.
#### ImportStringTable
- Imports entries into the ResourceDB from files containing a ResourceStringTable.
#### ImportRaw
- Imports information and bitmap data from a specified platform's texture into a standardized format.
#### Autotest
- Runs automatic tests to ensure the application is working.
- When provided a path & format, will import, export, then reimport specified file to ensure IO parity.
