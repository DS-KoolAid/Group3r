# Building Group3r with GitHub Actions

Since this is a .NET Framework 4.5.1 project with Windows-specific dependencies, it's best compiled using GitHub Actions with Windows runners rather than trying to compile locally on Mac.

## Automatic Building

### On Every Push/PR
The project automatically builds on every push to `main`/`master` branches and on pull requests. The build artifacts are available for download from the Actions page.

### Manual Builds
You can trigger a manual build by:
1. Going to the **Actions** tab in your GitHub repository
2. Selecting the **Build Group3r** workflow
3. Clicking **Run workflow**

## Creating Releases

To create a release with compiled binaries:

1. Create and push a tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. GitHub Actions will automatically:
   - Build the project on Windows
   - Create a release
   - Upload compiled binaries as a release asset

## What Gets Built

The GitHub Actions workflow will:

1. **Restore NuGet packages** including the newly added Newtonsoft.Json
2. **Compile the project** using MSBuild on Windows
3. **Create artifacts** containing:
   - `Group3r.exe` - Main executable
   - All required `.dll` files
   - Configuration files
   - JSON documentation and examples

## Downloading Built Binaries

### From Actions (Development Builds)
1. Go to the **Actions** tab
2. Click on a successful build
3. Download the **Group3r-Release** artifact
4. Extract and run `Group3r.exe`

### From Releases (Tagged Versions)
1. Go to the **Releases** section
2. Download the latest release package
3. Extract and run

## JSON Output Feature

The compiled binary will include the new JSON output feature:

```bash
# Download and extract the built binary, then:
Group3r.exe -p json -s -y "C:\Path\To\SYSVOL"
```

## Benefits of This Approach

1. **No local Windows VM needed** - Builds happen in the cloud
2. **Consistent build environment** - Same Windows image every time
3. **Automatic releases** - Tag and get a release automatically
4. **Cross-platform development** - Develop on Mac, build on Windows
5. **CI/CD ready** - Integrated testing and deployment pipeline

## File Structure

```
.github/
└── workflows/
    ├── build.yml     # Builds on every push/PR
    └── release.yml   # Creates releases on tags
``` 