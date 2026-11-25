# PSX Modular Characters

Six everyday low poly humans in PSX style, rigged and ready to drop into a game: casual, sporty and office outfits with backpacks, glasses, headphones and bags. Free GLB files with one small pixel-art atlas each.

## Contents

- **6 characters** as GLB and FBX files (913-1,035 triangles each).
- One material and one 128 x 128 RGB atlas per character (Nearest filtering in the GLB).
- Rig: 18-bone FK humanoid: Root, Pelvis, Spine, Chest, Neck, Head, plus UpperArm, Forearm, Hand, Thigh, Shin and Foot on each side (.L / .R).
- Previews: one transparent PNG preview per character.

## Folder layout

```
Export/         GLB 2.0 files: Characters/
Export/FBX/     The same models as binary FBX, in the same subfolders, textures embedded
Textures/       Atlas PNG files (the same images are embedded in the GLB and FBX files)
Previews/       One transparent PNG preview per character
Documentation/  Import guide and measured asset inventory
```

Each character is split into separate meshes (head, hair, top, bottom, shoes, accessories) that share one material and one skeleton.

## Characters

| Character | Notes | Triangles | Bones |
|---|---|---:|---:|
| Caio | graphic T-shirt, jeans, sneakers, backpack | 967 | 18 |
| Iara | jacket, cargo, boots, glasses | 981 | 18 |
| Rui | dress shirt, dress trousers, shoes, watch | 913 | 18 |
| Lia | tank top, track pants, sneakers, headphones | 1,035 | 18 |
| Nilo | hoodie, shorts, sneakers, cap | 981 | 18 |
| Ines | striped top, skirt, sandals, shoulder bag | 929 | 18 |

Triangle counts were measured on the GLB files and include every visible part and equipped prop.

## Technical details

- Units: metres, glTF convention (+Y up), feet at the origin.
- One material per character: albedo atlas, roughness 1, no specular or normal maps. Flat shading.
- Textures use nearest-neighbour filtering. Keep *Closest* / *Nearest* / *Point* filtering in your engine for the PSX look.
- FBX: binary, textures embedded, -Z forward / Y up, scale applied (1 unit = 1 m). FBX does not store texture filtering: set it to *Point* / *Nearest* after import.
- All textures are embedded; the GLB files have no external dependencies.

## Known limitations

- No animation clips are included; the rig is delivered in rest pose.
- Long hair, bags and skirts have limited range in extreme twists and very wide strides.

## License

Cubicle Studio asset license (see LICENSE.txt).

(c) 2026 Cubicle Studio
