# Unity 3D Asset Setup and Licensing

The repository intentionally excludes third-party imported source assets from Unity Asset Store.

## PHOENIX GT

- Asset: [PHOENIX GT by XtreemARTS](https://assetstore.unity.com/packages/3d/vehicles/land/phoenix-gt-424544)
- Unity Asset Store license: publisher's applicable EULA (standard EULA shown on the current listing).
- Open the Unity project with Editor `6000.3.28f1`.
- Download and import the car package using **Package Manager → My Assets**.
- Ensure the `Sport Car` folder/prefab is imported with its material and `.meta` information.
- Open the V5 scene; if the prefab reference is missing, re-run the original cockpit installer menus from the project's `HVAC` menu and save the scene again.

**Why is it not on GitHub?** Publicly distributing the imported Unity Asset Store source files would require appropriate distribution permission, even when the asset is free to download. Cloners should import it independently.

## Unity project version control

Track `Assets/` (except licensed third-party imports), `Packages/`, `ProjectSettings/`, and associated `.meta` files. Do not upload `Library/`, `Temp/`, `Logs/`, `Obj/`, `Builds/`, or local settings and caches.

If other external packages have been imported manually into `Assets/`, review their redistribution rights and exclude them too.
