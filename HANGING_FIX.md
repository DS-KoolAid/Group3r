# Group3r Hanging Fix Documentation

## Problem Description

Group3r would hang indefinitely when run through `CreateProcessWithLogonW` (RunAs) with output redirection. This occurred because:

1. The main thread blocks on `mq.Pop()` waiting for messages
2. Complex process redirection through multiple layers (Group3r → cmd.exe → file) can cause deadlocks
3. The message processing thread might get blocked, preventing the `FinishMessage` from being processed
4. `Console.ReadLine()` in error handler blocks in non-interactive environments

## Solutions Implemented

### 1. Direct File Output Mode (Primary Fix)
- Created `DirectFileJsonProcessor.cs` that writes JSON directly to file without blocking
- Bypasses the message queue entirely when outputting JSON to a file
- Processes messages asynchronously to prevent blocking
- Ensures JSON array is properly closed even on error

### 2. Non-Blocking Message Queue
- Changed blocking `mq.Pop()` to `mq.Q.TryTake()` with 5-second polling
- Prevents infinite blocking while still allowing normal operation
- No hard timeout - the process runs until completion

### 3. Force Exit After Completion
- Added `Environment.Exit(0)` after JSON array is closed
- Ensures all output streams are flushed before exit
- Prevents process from hanging after successful completion

### 4. Removed Interactive Prompts
- Removed `Console.ReadLine()` that blocks in non-interactive environments
- Replaced with `Environment.Exit(1)` on errors

## How to Build

Run the build script:
```powershell
.\build_group3r.ps1
```

Or manually:
```bash
msbuild Group3r.sln /p:Configuration=Release
# or
dotnet build Group3r.sln -c Release
```

## Testing

The fixed version should:
- Complete successfully when run with RunAs and output redirection
- Properly close the JSON array even if interrupted
- Exit cleanly without hanging
- Work with all authentication methods (current user, RunAs, credentials)

## Usage Remains Unchanged

```bash
# Direct execution
Group3r.exe -p json -f output.json

# With credentials
Group3r.exe -p json -f output.json -u domain\user -d target.domain

# The aegis wrapper script works without modifications
``` 