# Volatility
![Logo](volatility_logo.png)
### Burnout Paradise platform-agnostic resource interface

## Installation
### Users
Download and extract the latest version of the application from the [Releases page](https://github.com/BurnoutHints/volatility/releases/latest).

### Developers
Ensure you have the necessary prerequisites to develop .NET 9.0 applications on your machine.

After cloning, initialize the extractor submodule with `git submodule update --init --recursive`.
This must stay recursive because `tools/libbndl-extractor` itself pins `Bo98/libbndl` as a nested submodule.

Compiling the application is as simple as opening the project within your IDE of choice (Such as Rider or Visual Studio 2022), or by running `dotnet build`.

The autotest workflow uses the `tools/libbndl-extractor` submodule to unpack bundle files without committing its generated CMake state and fetched dependencies into the main repo.

## Commands
NOTE: This may not be entirely comprehensive. Run "help" for a full list of commands within the application.

#### ImportResource
- Imports information and data from a specified platform's binary resource into a standardized text-based YAML format.
#### ExportResource
- Exports a specified YAML text-based resource to a specified platform's binary resource file.
#### PortTexture
- Ports texture data from the specified source platform's binary format directly to the specified destination platform's format.
#### ImportStringTable
- Imports entries from files containing a [ResourceStringTable](https://burnout.wiki/wiki/Bundle_2/Burnout_Paradise) into the ResourceDB.
#### Autotest
- Runs automatic tests to ensure the application is working.
- When provided a path & format, will import, export, then reimport specified file to ensure IO parity.
- When provided game path(s), roundtrip tests now include exact binary parity checks between original and exported files.
