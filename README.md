# Vacuous's Editor Tools

Editor workflow tools for Unity 6.4 and newer. Everything is editor-only: nothing here ends up in a build.

## Install

In Unity, open **Window > Package Manager > + > Install package from git URL** and enter:

```
https://github.com/Vvacuous/vacuous-editor-tools.git
```

To pin a version, add a tag: `https://github.com/Vvacuous/vacuous-editor-tools.git#v0.1.0`.

## Tools

| Tool | Where | What it does |
| --- | --- | --- |
| Animator Clip Packer | Tools > Vacuous > Animation | Packs selected clips into an Animator Controller as sub-assets, or unpacks them back to `.anim` files and rewires the states. |
| Auto Material From Textures | Assets > Create > Vacuous | Builds URP Lit materials from texture sets named `<base>_<map>` (`albedo`, `normal`, `ao`, `metallic`, `height`, `emission`). `tex_crate_albedo` becomes `mat_crate`. |
| Pose Baker | Tools > Vacuous > Characters | Poses a character on any frame of a clip and bakes a static copy for screenshots and Recorder. |
| Mesh Read/Write | Assets > Vacuous > Meshes | Reports readable meshes in the selected folders and clears Read/Write on standalone mesh assets. |
| Default Scene | Assets > Create > Scene > Scene (Default Template) | Creates a scene from the template set in Project Settings. |
| Open Source File | Assets > Vacuous > Open Source File | Opens the `.blend` (or other source file) a model was exported from. |
| Terrain to OBJ | Tools > Vacuous > Terrain | Exports a Terrain's heightmap as an OBJ mesh. |
| Inspector Lock | Tools > Vacuous > Workflow (Alt+R) | Toggles the lock on the focused Inspector. |
| Rule of Thirds Guide | Tools > Vacuous > Camera | Draws a thirds grid over the Game view. Only compiles when Cinemachine 3 is installed. |

## Settings

**Project Settings > Vacuous Editor Tools** holds the per-project values, saved in `ProjectSettings/VacuousEditorToolsSettings.asset` so the team shares them:

- **Default Scene Template** and **New Scene Name**, used by Default Scene.
- **Models Folder**, **Source Art Folder** and **Source File Extension**, used by Open Source File. A model at `<Models Folder>/Props/mesh_crate.fbx` opens `<Source Art Folder>/Props/mesh_crate.blend`, with the source art folder relative to the project root.

## Requirements

- Unity 6000.4 or newer.
- Auto Material From Textures targets URP Lit.
- Rule of Thirds Guide needs Cinemachine 3; without it the rest of the package still works.

## License

MIT. See [LICENSE.md](LICENSE.md).
