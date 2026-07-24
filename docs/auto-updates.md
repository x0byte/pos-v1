# POS Auto Updates

## Previous Manual Workflow

The old deployment process was:

1. Open the .NET project in Visual Studio.
2. Publish the app into a folder.
3. Zip the published folder.
4. Upload the zip to Google Drive or similar storage.
5. Connect to the client computer through AnyDesk.
6. Download the zip.
7. Manually uninstall or remove the previous version.
8. Manually install or copy the new version.

That process still starts with a normal publish folder, but the zip/manual install step is replaced by Velopack packaging and an admin-triggered update screen.

## New Velopack Workflow

The new workflow is:

1. Publish the app to a folder.
2. Package that folder with `vpk pack`.
3. Upload the Velopack release files to an update feed location.
4. Configure the POS app with that update feed URL or path.
5. On the client machine, an admin opens `Updates`, checks for an update, downloads it, and clicks `Install and Restart`.

Updates are not installed silently. The app blocks install if a bill is open, a cashier session is active, or local fallback sync events are pending.

## Install The Velopack CLI

Install the `vpk` tool on the developer machine:

```powershell
dotnet tool install -g vpk
```

Update it later with:

```powershell
dotnet tool update -g vpk
```

## Publish The App

Use the existing Visual Studio publish-folder workflow for now:

1. Open `POS.sln`.
2. Select the `STC_POS` project.
3. Publish to a clean local folder.
4. Confirm the folder contains the main executable, currently `WindowsFormsApp1.exe`.

## Package A Release

Run `vpk pack` against the publish folder:

```powershell
vpk pack --packId <CompanyName.POS> --packVersion <version> --packDir <publish-folder> --mainExe <main-exe-name>
```

Example:

```powershell
vpk pack --packId BlackBox.POS --packVersion 1.0.1 --packDir C:\Builds\POS\publish --mainExe WindowsFormsApp1.exe
```

Velopack creates release files such as packages, installer files, and `releases.win.json` in the output directory.

Use the same `packId` for every release. Increase `packVersion` for each production release.

## Upload Release Files

Upload the complete Velopack release output to the configured feed location. The feed can be:

- A local or network folder, for example `C:\POSUpdates` or `\\server\share\POSUpdates`.
- An HTTP endpoint, for example `https://updates.example.com/pos`.
- A static file host, object storage bucket, or similar location that serves the release files.

Do not upload only the installer. The update feed needs the generated release metadata and package files.

## Configure The Update Feed URL

Set the feed in `WindowsFormsApp1.exe.config` after install:

```xml
<appSettings>
  <add key="VelopackFeedUrl" value="https://updates.example.com/pos" />
</appSettings>
```

For local testing, use a folder path:

```xml
<add key="VelopackFeedUrl" value="C:\POSUpdates" />
```

If the value is empty or missing, the Updates screen shows:

```text
Update feed is not configured.
```

## Test With Two Local Versions

Use a local folder as the update feed before using a client machine.

1. Publish version `1.0.0` to `C:\Builds\POS\1.0.0`.
2. Package it:

```powershell
vpk pack --packId BlackBox.POS --packVersion 1.0.0 --packDir C:\Builds\POS\1.0.0 --mainExe WindowsFormsApp1.exe --outputDir C:\POSUpdates
```

3. Install the generated setup from `C:\POSUpdates`.
4. Configure the installed app's `WindowsFormsApp1.exe.config` with:

```xml
<add key="VelopackFeedUrl" value="C:\POSUpdates" />
```

5. Publish version `1.0.1` to `C:\Builds\POS\1.0.1`.
6. Package it to the same feed folder:

```powershell
vpk pack --packId BlackBox.POS --packVersion 1.0.1 --packDir C:\Builds\POS\1.0.1 --mainExe WindowsFormsApp1.exe --outputDir C:\POSUpdates
```

7. Open the installed POS app as an admin.
8. Open `Updates`.
9. Click `Check for Updates`.
10. Click `Download Update`.
11. Make sure no bill is open, no cashier session is active, and no unsynced fallback bills exist.
12. Click `Install and Restart`.

## Roll Back A Bad Release

Preferred rollback:

1. Fix the issue.
2. Publish a new version with a higher version number.
3. Package and upload the new release.
4. Ask admins to update through the Updates screen.

Emergency rollback:

1. Remove the bad release files from the update feed so more clients do not install it.
2. Repackage the last known good build with a higher version number.
3. Upload that package to the feed.
4. Update affected clients through the Updates screen.

Do not reuse an old version number for rollback. Velopack update checks expect version numbers to move forward unless downgrade options are intentionally enabled.
