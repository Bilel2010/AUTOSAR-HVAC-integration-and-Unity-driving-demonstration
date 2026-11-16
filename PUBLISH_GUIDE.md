# Publish AUTOSAR HVAC V5 to Bilel2010/My-project (Windows PowerShell)

> The GitHub repository already contains a Unity starter project on `main`. This procedure preserves that history and publishes an update through a dedicated branch / pull request.

## Step 1 — Clone the GitHub repository

```powershell
Set-Location 'D:\AUTOSAR_HVAC_Project'
git --version
git clone 'https://github.com/Bilel2010/My-project.git' 'My-project-GitHub'
Set-Location 'D:\AUTOSAR_HVAC_Project\My-project-GitHub'
git switch -c release/autosar-hvac-v5
```

If the clone directory already exists, stop before recloning and inspect it with `git status` and `git remote -v`.

## Step 2 — Copy the Unity sources from the working project

```powershell
$source = 'D:\AUTOSAR_HVAC_Project\Unity\My project'
$repo = 'D:\AUTOSAR_HVAC_Project\My-project-GitHub'

# Exclude PHOENIX GT and imported Asset Store source code.
robocopy "$source\Assets" "$repo\Assets" /E /XD "$source\Assets\Sport Car" "$source\Assets\AssetStoreTools" /XF *.unitypackage

# Copy Unity package manifests and project configuration.
Copy-Item "$source\Packages\*" "$repo\Packages" -Recurse -Force
Copy-Item "$source\ProjectSettings\*" "$repo\ProjectSettings" -Recurse -Force
```

`robocopy` exit codes 0–7 commonly indicate success or partial copied/skipped differences; a nonzero status is not automatically failure. After copying, **inspect** the new files before staging. If additional third-party assets were installed under other directories, remove them from the staging copy as appropriate.

**Important:** The public clone may show the vehicle prefab as missing until PHOENIX GT has been downloaded/imported independently. Explain this in the README.

## Step 3 — Add MATLAB model files and bridge sources

Create `matlab/models/` and `matlab/bridge/` in the clone. Copy the validated project files (`HVAC_Control_SWC_AUTOSAR_v3_FaultHandling.slx`, `HVAC_Thermal_Plant_Discrete.slx`, `HVAC_ROS2_LinkWatchdog_v1.slx`) into `matlab/models/` from their actual local locations.

For the known bridge directory:

```powershell
New-Item -ItemType Directory -Force "$repo\matlab\models" | Out-Null
New-Item -ItemType Directory -Force "$repo\matlab\bridge" | Out-Null
Copy-Item 'D:\AUTOSAR_HVAC_Project\WindowsBridge\MATLAB\*.m' "$repo\matlab\bridge\" -Force
```

Also copy any ARXML exports that are appropriate for distribution to `matlab/autosar/`. Do not copy `slprj`, simulation caches, local secrets, unnecessary checkpoints or entire MATLAB tool installations.

## Step 4 — Add the provided documentation

From the downloaded release pack copy `README.md`, the `docs/` folder and `PUBLISH_GUIDE.md` to the root of the GitHub clone. **Append** the lines in `GITIGNORE_APPEND.txt` to the existing `.gitignore`; do **not** overwrite it. You can open both text files in VS Code and paste the appended lines, then save `.gitignore`.

## Step 5 — Review before publication

```powershell
Set-Location 'D:\AUTOSAR_HVAC_Project\My-project-GitHub'
git status --short
git add Assets Packages ProjectSettings matlab docs README.md PUBLISH_GUIDE.md .gitignore .gitattributes
git diff --cached --stat
git diff --cached --name-only
```

**Stop and remove any staged proprietary asset** or personal secret before committing. The staged list should not contain `Assets/Sport Car/`, `Library/`, backup datasets, or credential files. Check all staged files for unexpected large binaries.

```powershell
git commit -m "feat: add AUTOSAR HVAC Simulink-ROS2-Unity demo"
git push -u origin release/autosar-hvac-v5
```

If authentication is requested, sign into GitHub using your normal Git credential manager; do not paste access tokens into chat.

## Step 6 — Open a pull request and merge

Visit: `https://github.com/Bilel2010/My-project/compare/main...release/autosar-hvac-v5`

Suggested title: `Release: AUTOSAR HVAC integration and Unity driving demonstration`

PR description: `Adds Unity V5 scene and scripts, ROS 2/MATLAB bridge and curated Simulink models; documents validation results, setup, external asset dependencies and remaining limitations.`

Review the Files changed tab and merge to `main` only when the changes are correct.

## Git LFS (only if necessary)

Large `.slx` files may justify Git LFS. Install Git LFS locally, then *before* staging the models run `git lfs install` and `git lfs track "*.slx"`; commit the modified `.gitattributes`. Do **not** use LFS as a workaround to redistribute third-party assets without permission.
