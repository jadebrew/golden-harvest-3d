# Import guide - PSX Modular Characters

## Godot 4

1. Copy the GLB files from `Export/Characters` into your project.
2. In the Import dock keep the default *Scene* import. The skeleton, skinned meshes, material and embedded atlas come in with the file.
3. Set texture filtering to *Nearest* (Project Settings > Rendering > Textures > Default Texture Filter, or per material) to keep the pixel look.

## Unity

Drop the FBX files from `Export/FBX` into Assets, or use the GLB files with **glTFast** (`com.unity.cloud.gltfast`). Set the atlas texture Filter Mode to *Point* and Compression to *None*. Unity import was not tested for this release.

## Unreal Engine

Import the FBX files from `Export/FBX` (or the GLB files with the glTF importer). Set the texture filter to *Nearest*. Unreal import was not tested for this release.

## Blender

- Import a GLB with *File > Import > glTF 2.0*, or an FBX with *File > Import > FBX*.
- When importing GLB files into Blender, enable *Disable Bone Shape* to avoid an extra display sphere on the rig.

## Inventory

`Asset_Inventory.json` lists every GLB and FBX in this pack with measured triangles, bones, clips, texture size and SHA-256 hash.
