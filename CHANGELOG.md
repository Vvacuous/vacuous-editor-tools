# Changelog

## [0.1.1] - 2026-10-22

### Added
- MIT license.

### Changed
- README no longer says the repository is private.

## [0.1.0] - 2026-10-22

### Added
- Animator Clip Packer, Auto Material From Textures, Pose Baker, Mesh Read/Write, Default Scene, Terrain to OBJ, Inspector Lock and Rule of Thirds Guide, moved from earlier project tooling.
- Open Source File: opens a model's source file from the mirrored source art folder.
- Project Settings > Vacuous Editor Tools page for the scene template and source art mapping.

### Changed from the earlier versions
- All tools live in the `Vacuous.EditorTools` namespace with menus under Vacuous.
- Default Scene reads its template from Project Settings instead of a hard-coded path.
- Mesh Read/Write works on the selected folders instead of one hard-coded folder.
- Auto Material names materials `mat_<name>` and supports emission maps.
- Rule of Thirds Guide moved to its own assembly that only compiles with Cinemachine 3.
