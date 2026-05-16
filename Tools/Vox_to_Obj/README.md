# Vox_to_Obj

把 MagicaVoxel `.vox` 文件转成 Wavefront `.obj` 几何 + `.mtl` 材质 + 256x1 的调色板 `.png` 纹理，方便导入 Blender / Unreal / Unity 等。

输出几何保留每个体素的原始顶点（不做合并、不做三角化），方便后续在 3D 软件里编辑。

---

## 环境要求

- Python 3.8+
- Pillow（仅在使用单文件模式时需要，用来写调色板 PNG）

```
pip install Pillow
```

---

## 三种使用方式

### 1. 单文件模式（最常用）

给一个或多个 `.vox` 全路径，每个 VOX 会在**同目录下**生成三个同名的伴随文件：

```
python vox_to_obj_exporter.py <vox_path> [<vox_path>...]
                              [--scale S] [--no-center] [--no-ground]
```

例：

```
python vox_to_obj_exporter.py "D:\...\Icons\009\Icon_009_01.vox"
```

输出（落在 `D:\...\Icons\009\` 下）：

```
Icon_009_01.obj    几何 + mtllib/usemtl 引用同名 .mtl
Icon_009_01.mtl    材质：map_Kd 指向同名 .png
Icon_009_01.png    256x1 调色板纹理（每个像素对应一个 palette index）
```

#### 坐标变换选项

| 参数 | 默认 | 作用 |
|------|------|------|
| `--scale S` | `0.1` | 每个体素对应几个世界单位。`0.1` 是 MagicaVoxel 自带导出器的默认值，跟 `Icons/001/*.obj`（MV 原生导出）的比例一致 |
| `--no-center` | 默认会居中 | 默认沿 X 和 Y 把模型 bbox 中心放到原点；加这个 flag 改为保留原始体素坐标 |
| `--no-ground` | 默认会落地 | 默认把最低的 Z 体素拉到 Z=0（"角色脚踩地面"）；加这个 flag 改为不调整 Z |
| `--y-up` | 默认 **关** | 默认保留 vox 的 Z-up 约定。加这个 flag 后做 -90° 绕 X 轴旋转，把 vox Z（上）映射到 OBJ Y。**Unity / Three.js / 任何 Y-up 引擎都需要这个 flag**，不然角色会"趴在地上、头朝屏幕外面" |

**在 Unity 里用 → 一律加 `--y-up`**

```
python vox_to_obj_exporter.py Icon_009_01.vox --y-up
# scale=0.1  center=true  ground=true  y_up=true
# 输出 X 居中 / Y 朝上从 0 起 / Z 居中（深度）
```

这样输出的 OBJ 跟 MagicaVoxel 自带导出器的轴布局完全一致：X 左右、Y 上下、Z 前后深度。法线也跟着旋转，所以 Unity 的光照不会反。

只要不写 `--y-up`，输出就是 vox 的原始 Z-up 约定（每个体素 6 个面的"上面"法线是 `(0, 0, 1)`），适合不在乎"Y 是 up"的工具链。

最常见的两种调用：

```bash
# 给 Unity 用（Y-up，跟 MV 原生导出对齐）
python vox_to_obj_exporter.py path/to/foo.vox --y-up

# 拿回纯体素坐标（旧的"原始"行为）
python vox_to_obj_exporter.py path/to/foo.vox --scale 1.0 --no-center --no-ground
```

一次性处理一个文件夹下的所有 VOX，用 shell glob 即可：

PowerShell:
```powershell
python vox_to_obj_exporter.py (Get-ChildItem "D:\...\Icons\009\*.vox").FullName
```

Bash / Git Bash:
```bash
python vox_to_obj_exporter.py /d/.../Icons/009/*.vox
```

控制台输出形如：

```
scale=0.1  center=true  ground=true
Icon_009_01.obj  (10176 verts, 2544 quads)
  + Icon_009_01.mtl
  + Icon_009_01.png
```

### 2. 批量模式（通过 `exporter.txt`）

如果命令行没传任何 `.vox` 路径，脚本会去同目录读 `exporter.txt`。文件格式（共三行）：

```
<源目录绝对路径>
<目标目录绝对路径>
true | false        ← 是否开启 greedy mesh 优化（目前是 TODO，无效果）
```

示例 [exporter.txt](exporter.txt)：

```
D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Menu\vox
D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Menu\obj
true
```

行为：递归扫描源目录下的所有 `.vox`，在目标目录镜像源的目录结构、并写 `.obj` 文件。**这种老模式只输出 `.obj`，不写 `.mtl` 和调色板 `.png`。** 如果你要这两个文件，请用单文件模式。

### 3. 交互模式（fallback）

如果 `exporter.txt` 也不存在，脚本会进入交互模式，提示你输入：

1. 输出目录路径
2. 是否优化（y/n）
3. 一系列 glob 模式，按一次回车（空行）或输入 `exit` 退出

这条路径同样只产 `.obj`。

---

## 输出文件细节

### `.obj`

- 包含 `mtllib <name>.mtl` 和 `usemtl palette` 两行（仅单文件模式）
- 6 个法线（轴对齐）+ texcoords + 每个体素 1×1×1 cube 的暴露面
- 顶点是整数坐标，未合并；面是 quad（不是 triangle），保持体素结构方便后期编辑
- UV 公式：`u = (color_index - 1) / 256 + 1/512`，`v = 0.5` — 对应调色板 PNG 中的第 `color_index - 1` 个像素

### `.mtl`

最小化的 Wavefront 材质：

```
newmtl palette
illum 1
Ka 0.000 0.000 0.000
Kd 1.000 1.000 1.000
Ks 0.000 0.000 0.000
map_Kd Icon_xxx_xx.png
```

### `.png`

256 像素宽、1 像素高的 RGBA 调色板。第 `i` 个像素就是 VOX 中 palette index `i+1` 的颜色（MagicaVoxel 的索引约定）。VOX 文件如果没有 `RGBA` chunk，PNG 默认填白色。

---

## 测试 / 已验证示例

`D:\SourceCode\Git\toneyisnow\windingtale\Resources\Remastered\Icons\009\` 下的 3 个 VOX（`Icon_009_01.vox` / `_02.vox` / `_03.vox`）已经用单文件模式跑过，输出了对应的 `.obj` / `.mtl` / `.png`，可以在该目录直接看到。

复现命令：

```bash
cd /d/SourceCode/Git/toneyisnow/windingtale/Tools/Vox_to_Obj
python vox_to_obj_exporter.py \
  /d/SourceCode/Git/toneyisnow/windingtale/Resources/Remastered/Icons/009/Icon_009_01.vox \
  /d/SourceCode/Git/toneyisnow/windingtale/Resources/Remastered/Icons/009/Icon_009_02.vox \
  /d/SourceCode/Git/toneyisnow/windingtale/Resources/Remastered/Icons/009/Icon_009_03.vox
```

---

## 与 MagicaVoxel 自带导出器的差异

| 项 | MagicaVoxel 导出 | 本脚本（单文件模式） |
|---|---|---|
| 顶点合并 | 自动 greedy / monotone | 不合并，每个体素 6 个 quad 都独立 |
| 坐标 scale | 0.1 / voxel | 默认 `0.1` / voxel（可改 `--scale`） |
| 居中 / 落地 | 自动（按 SIZE chunk 中心） | 默认开启（按 bbox 中心，模型脚踩 Z=0） |
| 上轴 | Y up（导出时绕 X 转 -90°） | 默认 Z up；加 `--y-up` 后跟 MV 一致 |
| `.mtl` / 调色板 | 自动生成 | 自动生成（格式略简化） |
| 可编辑性 | 三角化后顶点丢失结构 | 体素结构保留，适合在 Blender 里手动改 |

如果只是单纯渲染、不需要再编辑，MagicaVoxel 自带导出器更合适；如果要去 Blender 里基于体素拓扑做修改、或者批量做体素自动处理，用本脚本。

---

## 已知限制

- `optimizing` / greedy mesh 路径是 TODO，目前没有真正实现合并
- `importVox()` 的旧实现忽略 palette；想拿调色板必须用 `importVoxFull()`（单文件模式内部已经用了）
- VOX 版本必须是 150（MagicaVoxel 当前版本）
- 体素模型的整数坐标不会重新居中，导入 Blender 后可能要手动调整 origin
