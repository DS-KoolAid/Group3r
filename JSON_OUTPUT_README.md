# JSON Output Support for Group3r

This document explains how to use the JSON output feature in Group3r.

## Overview

Group3r now supports JSON output format in addition to the default human-readable format. This allows for easier integration with other tools and automated processing of results.

## Usage

To enable JSON output, use the `-p` or `--printer` option with the value `json`:

```bash
# JSON output
Group3r.exe -p json [other options]

# Default human-readable output (nice format)
Group3r.exe -p nice [other options]
# or simply
Group3r.exe [other options]
```

## Examples

### Basic JSON output to console
```bash
Group3r.exe -p json -s -y "C:\Path\To\SYSVOL"
```

### JSON output to file
```bash
Group3r.exe -p json -f output.json -y "C:\Path\To\SYSVOL"
```

### Offline mode with JSON output
```bash
Group3r.exe -p json -o -s -y ".\TestSysvol"
```

## JSON Output Structure

The JSON output includes:

### GPO Results
Each GPO result is output as a complete JSON object containing:
- **Attributes**: GPO metadata (name, path, version info, etc.)
- **GpoAclResult**: Parsed ACL information
- **GpoAttributeFindings**: Security findings related to GPO attributes
- **SettingResults**: Individual policy settings and their associated findings

### Other Messages
Non-GPO messages are output in the following format:
```json
{
  "Timestamp": "2024-01-01T00:00:00",
  "Type": "MessageType",
  "Message": "Message content"
}
```

### File Results
File findings are output with additional file result details:
```json
{
  "Timestamp": "2024-01-01T00:00:00",
  "Type": "FileResult",
  "Message": "Message content",
  "FileResult": {
    // File result details
  }
}
```

## Benefits of JSON Output

1. **Machine-readable**: Easy to parse and process with scripts or other tools
2. **Structured data**: Maintains the hierarchical structure of GPO data
3. **Complete information**: All findings and details are preserved
4. **Integration-friendly**: Can be piped to other tools or imported into databases

## Notes

- When using JSON output, debug and trace messages are suppressed to keep the output valid JSON
- Each GPO result is output as a separate JSON object
- The output uses indented formatting for readability
- Null values are omitted from the output
- Enums are serialized as strings for better readability 