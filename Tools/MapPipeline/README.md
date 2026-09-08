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
| `build_obstacles_08.py` | 第 08 关：29 格宽的整段城堡城墙（一个 obstacle）和骑士石像（`stone_statue_2` 是误读，已弃用） |
| `build_obstacles_09.py` | 第 09 关：石球石柱（复用 08 关的底座，第 08 关的两根也换成了它）和路中央的公告板 |
| `build_obstacles_10.py` | 第 10 关：三种火柱（矮 / 亮 / 略暗），每种两帧动画（`<key>.vox` + `<key>_f2.vox`） |
| `build_obstacles_13.py` | 第 13 关：两顶帐篷（灰绿 / 蓝白，同一形状两套颜色，自带调色板） |
| `build_trees.py` | 所有关卡共用的树：每种"颜色 + 树型"一个固定模型（`tree_<colour>.vox` / `pine_<colour>.vox`，1 格） |
| `tree_obstacles.py` | 从 ShapeMatrix 里把树读出来变成 obstacle 列表（每个树冠一棵，站在树干那一格），带按地面颜色选好的 `Fill` |
| `voxmesh.py` | 贪心合并同色共面体素面的 OBJ 导出器，给超过 10 格宽的模型用（也可 `--greedy` 强制） |
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

`--variant STRETCH,WIDTH`（可重复）加 `--variant-tiles 80,81,...` 会给这些 tile 再各写
一份 `Shape_<NN>_<id>_v<k>.vox`：树冠拉高到 STRETCH 倍、按中心重采样到 WIDTH 倍宽
（裁在 24×24 之内）。`ShapesLayer` 会在基础模型旁边找 `_v1`、`_v2`……，按格子坐标的
哈希在"基础 + 变体"里挑一个，这样第 09 关 200 格的红蓝松林不是同一棵树复制 200 次。
`install_chapter.py` 只检查基础模型存在。

### 一个 .vox 部件最宽 10 格

`.vox` 的每个坐标只占 1 个字节，所以文件里的一个模型不能超过 256 体素。
第 05 关的大教堂（19 格）和教堂（12 格）是拆成并排的两个 obstacle 做的，
拆的位置要让门廊整个落在其中一块里；背后那条长屋顶用 `housekit`/`build_obstacles_05`
里的 `hall()`，它的高度只跟 y 有关，两块拼起来接缝处才不会错开。

第 08 关的城墙 29 格宽、必须是**一个** obstacle，所以 `voxlib.write_vox` 现在接受任意
`SIZE`：超过 256 时按 240（10 格）切成多个部件，用 MagicaVoxel 的场景图
（nTRN > nGRP > nTRN > nSHP）记录每块的位移，`read_vox` 再拼回一个完整模型，
MagicaVoxel 打开也是一整面墙。这类模型 `vox_batch_to_obj.py` 会自动交给 `voxmesh.py`
导出（老导出器只读第一个部件）；`voxmesh` 还会把同色共面的体素面合并成大矩形，
城墙只有 1.5 万个面、1.6 MB，按老导出器会是 70 MB。`build_obstacles_08.py` 只把
外壳写进 `.vox`（实心会有 1400 万体素），`voxmesh.mesh()` 网格化前先用 scipy
把封闭的空腔填实，所以导出的 OBJ 没有内表面。

### 有动画的 obstacle：`<key>_f2.vox`

一个 obstacle 可以带多个模型：`<key>.vox` 是第 1 帧，`<key>_f2.vox`、`<key>_f3.vox`……
是后面的帧，和普通模型一样导出、一样拷进 Unity。`ObstaclesLayer` 会在模型旁边找
`_f2`、`_f3`……，全部挂在同一个 obstacle 下面，由 `ObstacleAnimation` 按全局常数
`FramesPerSecond` 轮流显示。第 10 关的火柱就是这样：每种两帧，顶上的火苗一帧高一帧矮。

每一帧的 `SIZE` 和 X/Y 方向的体素范围必须和第 1 帧一致——导出时按体素包围盒居中，
`ObstaclesLayer` 也是拿所有帧合起来的包围盒去定位、算 footprint 的。
做法是让模型最宽的那部分（火柱是底下的火盆）每帧完全一样，只改里面的东西。
`install_chapter.py` 只检查第 1 帧存在。

### 树也是 obstacle：`build_trees.py` + `tree_obstacles.py`

原画里每棵树画在上下两格：上面一格是树冠的圆锥（top），正下方一格是树冠下沿、树干和
树根（base）。树林就是一列 top 叠在一起、最底下一个 base——每个 top 就是一棵树，前面
那棵挡住后面那棵的下半截。所以：

* 每个 top 格 (X, Y) 是一棵树，站在 **(X, Y+1)**——树干画的那一行，和雕像 / 火柱
  "画两行站一行"是同一条规则；
* base 格由上面那棵树一起清理；上面没有 top 的 base（树冠在地图上边缘外）自己站一棵；
* 最后一行的 top，或者树干那一行不是树格的（房子后面露出来的树冠，第 01 关茅草屋
  角上有一棵），就站在自己这一格。

每种"颜色 + 树型"只有**一个**固定模型，不再随机：`tree_dark_green`、`tree_light_green`、
`tree_bright_green`……（`build_trees.py` 的 `TREES` 表，颜色 ramp 是从 tile 上采的，
模型自带调色板，因为 MagicaVoxel 默认调色板会把 7 级树冠颜色压成 3 级）。松树型
（第 17、20 关）是 `pine_<colour>`。哪些 tile 是 top / base、各是什么颜色，看图
定，然后：

```bash
python build_trees.py                                   # 只需一次，模型是所有关共用的
python tree_obstacles.py 04 \
    --top 128,130,137,142=tree_dark_green --top 131,133,141=tree_light_green \
    --top 132,136,138=tree_bright_green \
    --base 129=tree_dark_green --base 134=tree_light_green --base 139=tree_bright_green \
    --with obstacles/obstacles_04.json -o obstacles/obstacles_04_with_trees.json
python map_clean.py 04 --obstacles obstacles/obstacles_04_with_trees.json
```

`obstacles_NN.json` 仍然是手写的房子列表；`_with_trees.json` 是它加上推导出来的树，
`map_clean.py` 吃后者。每个树格的 `Clear` 都带 `Fill`：拿这个 tile 去掉树冠 / 树干 /
树根 / 阴影颜色之后剩下的像素，和每个普通地砖比颜色直方图，覆盖最多的那个
（平手取全图用得最多的），所以路边的树清掉后回来的是草而不是路。
`map_clean.py` 先把这些 `Fill` 涂上，再让其它 footprint 从它们往里长。
`--fill 74,75,88,89,92,93=41` 可以强行指定某些树格的 `Fill`：第 14 关的草地深浅不一、
林子里深绿 / 浅绿两种树逐格交替，按 tile id 各自匹配会得到两种草砖，清完后是一片棋盘格。

树的 tile 清掉之后就不再出现在 `UsedTiles` 里，`shapes_to_vox.py` 的 `--tree` 就不需要
了（第 01–09 关都已经这样改过；`--tree` / `--variant` 只留给还想在 tile 上盖树冠的场合）。
12 种树全部建好：深绿 / 浅绿 / 亮绿 / 蓝 / 深红 / 浅红 / 深灰 / 浅灰 / 雪蓝 / 雪红 +
雪松 / 蓝松。第 11–21 关还没做 3D 地图，它们的树 tile 已经按颜色分好类记在
`obstacles_prompt.md` 里，转地图时直接照抄那条命令。第 16 关每棵树的树冠都是同样的
白雪，颜色只在它下面那格露出来，所以用 `--colour-from-stand`。

画在房子后排上面的树冠（第 02 关大房子顶上那一排）：树在房子后面，站到再往后一行的
空地上；后面没地方就丢掉，不会长在屋顶里。

### 没有海岸线的关：`--flat`

`shapes_to_vox.py` 按颜色分地形（沙滩 / 水 沉下去）。第 10 关的洞窟没有沙滩，但地上
火圈的橙色落到调色板上正好是沙滩色，会把火圈沉下去一格——加 `--flat`，所有像素都按
陆地高度。

### 填充时排除某些 tile：`--fill-exclude`

`map_clean.py --fill-plain-only` 只让 Type 0 的地砖长进抠掉的格子，但宝箱也是 Type 0，
数量够多就能赢得投票（第 10 关有 8 个）。`--fill-exclude 112` 把它从候选里去掉。

### 清理矩形不等于模型矩形时：`Clear`

`map_clean.py` 默认清理 `Position` + `Size` 那个矩形。当画在图上的东西不是这个矩形——
雕像画了两行却只站一格、城墙的轮廓不是矩形、10 格宽的模型放进 12 格宽的画——
给这个 obstacle 写 `"Clear": [ {X, Y, Cols, Rows}, ... ]`（绝对格坐标），
就只清理这些矩形。`Position` 仍然决定模型放哪，也只有它会进 `Chapter_NN.json`。

### 为什么 obstacle 建模没有工具

识别"这里有一座房子、它占 4 行 6 列"是看图判断，不是算法；房子本身长什么样也一样。
所以 obstacle 的 VOX 是手写脚本 / MagicaVoxel 做出来的，
这里只提供 `vox_preview.py`（看模型对不对）和 `map_clean.py`
（尺寸不匹配会直接报错：`SIZE` 必须是 24 的整数倍）来兜底。
