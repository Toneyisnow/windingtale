# MapPipeline

把某一关的 2D 瓦片地图转成 3D 体素地图的工具集。配套的工作流写在
`.claude/skills/map-2d-to-3d/`，那里描述"怎么做判断"，这里只做"确定性的机械活"。

所有规则都是从手工做出来的第 01 关素材里逆向出来的
（`Resources/Original/Shapes/ShapePanel01/*.png` → `Resources/Remastered/Shapes/Shapes_01/vox/*.vox`），
并由 `validate_shapes.py` 持续校验：**96 个参考 tile 全部逐体素一致**。

## 环境

Python 3.8+ 和 Pillow：

```bash
pip install Pillow
```

## 坐标约定（已验证）

| 概念 | 约定 |
|---|---|
| `ShapeMatrix[x][y]` | 地图 X = x+1（列，从左往右）、Y = y+1（行，从上往下） |
| tile PNG `(px, py)` | 24×24，`py` 从**上**往下 |
| shape VOX `(x, y, z)` | `x = px`、`y = 23 - py`、`z` = 地形高度 |
| 地面高度 | 陆地 23、沙滩 22、水 21、深水 20 |
| obstacle VOX `SIZE` | `(列数 × 24, 行数 × 24, 高度)` |
| obstacle `Position` | footprint 左上角那一格（1 起算），部分出屏时可以 ≤ 0 |

"N x M tiles"在这个项目里读作 **N 行（纵深）× M 列（宽）**，
所以 4×6 的房子 = `SIZE (144, 96, h)`。

## 工具

| 脚本 | 作用 |
|---|---|
| `voxlib.py` | 公共库：`.vox` 读写、MagicaVoxel 调色板、颜色→索引、地形分类、路径 |
| `chapter_map.py` | `info` / `render` / `crop` / `verify`：看懂一关的地图数据，并把 ShapeMatrix 重新画回 PNG |
| `map_clean.py` | 按 obstacle 列表把 footprint 抠掉换成普通地砖，产出 `Chapter_NN_Cleaned.json` |
| `shapes_to_vox.py` | tile PNG → 40³ 的 `Shape_1_<id>.vox` |
| `validate_shapes.py` | 拿第 01 关做回归：重新生成并与参考 VOX 逐体素比对 |
| `vox_preview.py` | 把 `.vox` 渲染成正交 + 等轴测的 PNG 联系表，用来肉眼检查模型 |
| `vox_batch_to_obj.py` | 批量 `vox/` → `../obj/`（`.obj` + `.mtl` + 调色板 `.png`） |

### 典型顺序

```bash
cd Tools/MapPipeline

# 第一步：看懂这一关
python chapter_map.py info 02
python chapter_map.py render 02 --grid --labels -o out/ch02_grid.png
python chapter_map.py verify 02        # '#' 的地方就是画在瓦片上的 obstacle

# 第二步：抠掉 obstacle（obstacles_02.json 是人/skill 看图写出来的）
python map_clean.py 02 --obstacles obstacles_02.json --dry-run
python map_clean.py 02 --obstacles obstacles_02.json

# 第三步：生成 shape VOX
python shapes_to_vox.py 02 --used-tiles .../Chapter_02_UsedTiles.json --tree 71:44

# 收尾：导出 OBJ
python vox_batch_to_obj.py --shapes 02
python vox_batch_to_obj.py --obstacles
```

### 生成规则（`shapes_to_vox.py`）

1. 每个像素取 MagicaVoxel 默认调色板里**最接近**的颜色（只搜索 1..255，
   平局取小索引——这样纯黑落在 225 而不是 256，跟参考素材一致）。
2. 每个像素放一个体素，高度按地形分类：沙滩比陆地低 1、水再低 1、深水再低 1。
3. 深绿 `(51,102,0)` 是草：往上再叠 2 个同色体素，形成 3 格高的草。
4. 有树的 tile：地面**整块换成**平地草砖 `Shape_0_52`（2D 的树因此消失），
   然后从第 01 关的 6 棵参考树里挑一棵，把树冠原样盖上去。这类 tile 不长草。

第 01 关手工做好的 6 棵参考树，由矮到高：

| 参考 tile | 形状 | 树冠顶端 z |
|---|---|---|
| 46 | 扁平灌木丛 | 29 |
| 40 | 圆润小灌木 | 30 |
| 43 | 圆头树（默认） | 32 |
| 44 | 较高的圆头树 | 35 |
| 47 | 小的阶梯状针叶树 | 37 |
| 42 | 高的阶梯状针叶树 | 38 |

`--tree 71:44` 表示"tile 71 有树，用 44 号的树冠"，
`--tree 71:44@12,11` 再把树冠挪到 tile 内的 (12,11)。

### 为什么 obstacle 建模没有工具

识别"这里有一座房子、它占 4 行 6 列"是看图判断，不是算法；房子本身长什么样也一样。
所以 obstacle 的 VOX 是手写脚本 / MagicaVoxel 做出来的，
这里只提供 `vox_preview.py`（看模型对不对）和 `map_clean.py`
（尺寸不匹配会直接报错：`SIZE` 必须是 24 的整数倍）来兜底。
