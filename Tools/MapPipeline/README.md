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
| `housekit.py` | 建房子的公共零件：`wing` / `hall` / `arch` / `window` / 玫瑰窗 / 十字架 |
| `build_obstacles_02.py` | 第 02 关的 5 栋蓝顶教堂 |
| `build_obstacles_05.py` | 第 05 关的 7 栋房子（红顶大教堂、蓝顶教堂、红顶大宅、两栋小屋） |
| `build_obstacles_06.py` | 第 06 关新加的两个模型：中央大门的红顶教堂、码头上的木货箱 |
| `chapter_map.py` | `info` / `render` / `crop` / `verify`：看懂一关的地图数据，并把 ShapeMatrix 重新画回 PNG |
| `map_clean.py` | 按 obstacle 列表把 footprint 抠掉换成普通地砖，产出 `Chapter_NN_Cleaned.json` |
| `shapes_to_vox.py` | tile PNG → 40³ 的 `Shape_<NN>_<id>.vox`（源 PNG 前缀是 `NN-1`，差一位） |
| `install_chapter.py` | 把清洗后的地图作为 `RenderMatrix` 装进 `Chapter_NN.json`，`ShapeMatrix` 保持原样 |
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
# 抠掉的地方默认从四周长回来。如果这一关四周全是树（第 05 关），
# 加 --fill-plain-only，只让 Type 0（Plain）的地砖长进来

# 第三步：生成 shape VOX
python shapes_to_vox.py 02 --used-tiles .../Chapter_02_UsedTiles.json --tree 71:44

# 第四步：导出 OBJ
python vox_batch_to_obj.py --shapes 02
python vox_batch_to_obj.py --obstacles

# 第五步：装进游戏读的 Chapter_02.json
python install_chapter.py 02 --dry-run
python install_chapter.py 02
```

### 一关有两张地图

`Chapter_NN.json` 同时带着两张同尺寸的矩阵，**不能互相替代**：

| key | 哪张图 | 谁读 |
|---|---|---|
| `ShapeMatrix` | 原始画好的地图，不动 | 战斗逻辑：`ShapeType` 决定移动力消耗、AP/DP 加成、能不能进 |
| `RenderMatrix` | 抠掉 obstacle footprint 的地图 | 只有 `ShapesLayer`，用来决定这一格摆哪个 tile 模型 |

把清洗后的地图写进 `ShapeMatrix` 是错的：房子底下的格子会变成平地草砖，
房子就能穿行了（第 02 关清洗后的地图里 `Blocked` 格子是 **0** 个）。
房子挡路靠的是它底下那格原始瓦片，obstacle 模型只是布景。

### 生成规则（`shapes_to_vox.py`）

1. 每个像素取 MagicaVoxel 默认调色板里**最接近**的颜色（只搜索 1..255，
   平局取小索引——这样纯黑落在 225 而不是 256，跟参考素材一致）。
2. 每个像素放一个体素，高度按地形分类：沙滩比陆地低 1、水再低 1、深水再低 1。
3. 深绿 `(51,102,0)` 是草：往上再叠 2 个同色体素，形成 3 格高的草。
4. 有树的 tile：地面**整块换成**本关的平地草砖（`--grass-tile`，第 01 关是 52、
   第 02 关是 153；2D 的树因此消失），
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
`--tree 71:44@12,11` 再把树冠挪到 tile 内的 (12,11)，
`--tree 71:44#2c4c6c` 再把树冠的树叶染成这个颜色（明暗保留、树干不动）——
第 01 关那 6 棵参考树都是绿的，而第 05 关的林子是绿 / 蓝绿 / 秋红三色。

`--tree-stretch F` 把树冠按层复制拉高到 F 倍（第 04 关用 1.4），
画布的 Z 会自动长高以免被切顶（1.4 倍时是 40×40×45）；
导出 OBJ 时按 X/Y 居中、按最低体素落地，所以画布变高不影响别的东西。

### 一个模型最宽 10 格

`.vox` 的每个坐标只占 1 个字节，所以任何一个模型都不能超过 255 体素 = 10 格。
比这更宽的房子要拆成并排的两个 obstacle（第 05 关的大教堂 19 格、教堂 12 格），
拆的位置要让门廊整个落在其中一块里；背后那条长屋顶用 `housekit`/`build_obstacles_05`
里的 `hall()`，它的高度只跟 y 有关，两块拼起来接缝处才不会错开。

### 为什么 obstacle 建模没有工具

识别"这里有一座房子、它占 4 行 6 列"是看图判断，不是算法；房子本身长什么样也一样。
所以 obstacle 的 VOX 是手写脚本 / MagicaVoxel 做出来的，
这里只提供 `vox_preview.py`（看模型对不对）和 `map_clean.py`
（尺寸不匹配会直接报错：`SIZE` 必须是 24 的整数倍）来兜底。
