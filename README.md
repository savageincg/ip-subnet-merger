# VPN IP Table Manager

A .NET WPF application for managing IP addresses and subnets in CIDR notation. The application provides an intuitive interface for adding, managing, and exporting IP ranges with automatic duplicate detection and smart subnet merging.

## Features

- **CIDR Notation Support**: Add IP addresses and subnets in CIDR format (e.g., `192.168.1.0/24`) or plain IP addresses (e.g., `192.168.0.1`, automatically treated as `/32`)
- **Smart Merging**: Automatically removes subnet subsets when adding a larger network range
- **Dirty Data Parsing**: Extract IP addresses from text files using regex patterns, ignoring invalid data
- **Automatic Persistence**: Data is automatically saved to YAML file after each add/remove operation
- **Auto-load on Startup**: Application automatically loads saved data when started
- **Export Formats**:
  - CSV format (comma-separated single line)
  - Route commands format (multi-line Windows route ADD commands)
- **File Import**: Load text files into the input field for batch processing

## Requirements

- .NET 8.0 SDK or later
- Windows (WPF application)

## Project Structure

The solution consists of three projects:

```
VpnIpTable/
├── VpnIpTable.Core/              # Class library with core logic
│   ├── Models/
│   │   └── IpRange.cs
│   └── Services/
│       ├── CidrService.cs
│       └── YamlStorageService.cs
├── VpnIpTable.Core.Tests/        # Unit tests (xUnit)
│   └── Services/
│       ├── CidrServiceTests.cs
│       └── YamlStorageServiceTests.cs
└── VpnIpTable.UI/                # WPF application
    ├── App.xaml
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs
```

## Building the Project

1. Clone the repository
2. Open the solution in Visual Studio or use the command line:

```bash
dotnet restore
dotnet build
```

## Running the Application

```bash
dotnet run --project VpnIpTable.UI/VpnIpTable.UI.csproj
```

Or build and run the executable from `VpnIpTable.UI/bin/Debug/net8.0-windows/`

## Running Tests

```bash
dotnet test
```

## Usage

### Adding Addresses

1. Enter IP addresses or CIDR ranges in the input field (one per line or comma-separated)
2. You can paste "dirty" text containing IP addresses - the application will automatically extract valid addresses
3. Click **Add** to add the addresses to the list

**Supported formats:**
- CIDR notation: `192.168.1.0/24`
- Plain IP address: `192.168.0.1` (treated as `/32`)
- Mixed text: The application extracts valid IP addresses using regex

### Loading from File

1. Click **Load from File** button
2. Select a text file containing IP addresses
3. The file content will be loaded into the input field
4. Click **Add** to extract and add addresses

### Removing Addresses

1. Select an address from the list
2. Click **Remove Selected**

### Exporting Data

- **Export (CSV)**: Exports all addresses as a comma-separated single line
- **Export (Route)**: Exports all addresses as Windows route ADD commands:
  ```
  route ADD x.x.x.x MASK y.y.y.y 0.0.0.0
  ```

### Data Storage

- Data is automatically saved to `addresses.yaml` in the application directory
- Data is automatically loaded when the application starts
- The YAML file contains a list of CIDR addresses

## Technical Details

### Dependencies

**VpnIpTable.Core:**
- `YamlDotNet` - YAML file handling

**VpnIpTable.Core.Tests:**
- `xunit` - Unit testing framework

**VpnIpTable.UI:**
- Standard WPF libraries
- Reference to `VpnIpTable.Core`

### Key Features Implementation

- **CIDR Parsing**: Custom implementation using `System.Net.IPAddress`
- **Smart Merging**: Automatically detects and removes subnet subsets
- **Regex Extraction**: Uses regular expressions to extract IP addresses from text
- **YAML Storage**: Stores data in human-readable YAML format

## Examples

### Input Examples

```
192.168.1.0/24
10.0.0.0/8
172.16.0.0/16
192.168.0.1
```

Or mixed text:
```
Here are some IP addresses:
- 192.168.1.0/24
- 10.0.0.1
- 172.16.0.0/16
Some invalid data: 999.999.999.999 (will be ignored)
```

### Export Examples

**CSV Export:**
```
192.168.1.0/24,10.0.0.0/8,172.16.0.0/16
```

**Route Commands Export:**
```
route ADD 192.168.1.0 MASK 255.255.255.0 0.0.0.0
route ADD 10.0.0.0 MASK 255.0.0.0 0.0.0.0
route ADD 172.16.0.0 MASK 255.255.0.0 0.0.0.0
```

## License

This project is provided as-is for personal or commercial use.
