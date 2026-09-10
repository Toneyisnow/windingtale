# Obstacle prompts, per chapter

What is painted on each chapter's map, and the `DefinitionKey` each object must
use. **Objects that recur across chapters reuse the same key** — the model is
built once, into `Resources/Remastered/Obstacles/vox/`, and every chapter that
needs it just references it. Only add a new key when the object really is a new
shape.

Keys are lower-case letters, digits and underscores only, e.g.
`dwelling_house_1.vox`.

Footprints are given as **rows × columns** of tiles and include the object's
drop shadow.

## Trees (every chapter)

Every tree is a 1 × 1 obstacle, one fixed model per crown colour and shape,
derived from the tile matrix by `tree_obstacles.py` (see `chapter-obstacles`,
step 2b). The art paints a tree over a crown-top tile and a trunk/base tile
under it; the tree stands on the base row. Keys, by the colour names used in
the per-chapter list below:

| 颜色 | key | chapters |
|---|---|---|
| 深绿 | `tree_dark_green` | 1, 2, 3, 4, 5, 7, 8, 11, 12, 14, 15, 18 |
| 浅绿 | `tree_light_green` | 1, 2, 3, 4, 14, 15 |
| 亮绿 | `tree_bright_green` | 4 |
| 蓝 | `tree_blue` | 5, 7, 8, 9, 11, 12, 18, 19, 20 |
| 深红 | `tree_dark_red` | 5, 6, 7, 8, 9, 11, 12, 19 |
| 浅红 | `tree_light_red` | 5, 6, 7, 8, 11, 12 |
| 深灰 / 浅灰 | `tree_dark_gray` / `tree_light_gray` | 13, 14, 15, 21 (the brown / tan dead trees) |
| 雪蓝 / 雪红 | `tree_snow_blue` / `tree_snow_red` | 16 (snow-white cone, colour under it) |
| 雪松 / 蓝松 | `pine_snow` / `pine_blue` | 17 / 20 (the slim trunkless pine shape) |

All twelve models are built. Chapters 10 and 22-30 have no trees (22-25 confirmed: no tree tile on any of them).

### Tree tiles of the chapters not yet converted (11-21)

Read off the tile art (cone core colour at pixel (11, 16)), so that when each
map is converted the tree step is just this command. Tiles not listed here
that are `Type 2` are log piles and stumps (11: 142; 12: none).

| ch | `tree_obstacles.py` arguments |
|---|---|
| 11 | `--top 124,131=tree_dark_green --top 127,128=tree_blue --top 132,139=tree_dark_red --top 135,136=tree_light_red --base 125=tree_dark_green --base 129=tree_blue --base 133=tree_dark_red --base 137=tree_light_red` |
| 12 | `--top 128,135=tree_dark_green --top 85,131,132=tree_blue --top 136,143=tree_dark_red --top 139,140=tree_light_red --base 129=tree_dark_green --base 89,133=tree_blue --base 137=tree_dark_red --base 141=tree_light_red` (85 is a blue crown on a cliff top, `Type 1`) |
| 13 | `--top 88,95=tree_light_gray --top 91,94=tree_dark_gray --base 89=tree_light_gray --base 92=tree_dark_gray` |
| 14 | `--top 74,87=tree_light_gray --top 75,86=tree_dark_gray --top 88,101,104,117=tree_dark_green --top 89,100,105,116=tree_light_green --base 78=tree_light_gray --base 79=tree_dark_gray --base 92,108=tree_dark_green --base 93,109=tree_light_green --fill 74,75,88,89,92,93=41` |
| 15 | `--top 72,80,85,88=tree_dark_green --top 73,81,84,89=tree_light_green --top 90,98,103=tree_light_gray --top 91,99,102=tree_dark_gray --base 76,92=tree_dark_green --base 77,93=tree_light_green --base 94=tree_light_gray --base 95=tree_dark_gray` |
| 16 | `--colour-from-stand --top 147=tree_snow_red --top 144,148,151=tree_snow_blue --base 145=tree_snow_red --base 149=tree_snow_blue` -- every crown is the same white cone; the colour shows in the tile *below* it (147 / 145 red, 151 / 149 blue), hence the flag |
| 17 | `--top 164,168=pine_snow --base 152,166,170=pine_snow` (152 is a single small pine) |
| 18 | `--top 128,135=tree_dark_green --top 131,132=tree_blue --base 129=tree_dark_green --base 89,133=tree_blue` |
| 19 | `--top 72,79=tree_blue --top 75,76=tree_dark_red --base 73=tree_blue --base 77=tree_dark_red` (none of these are `Type 2`) |
| 20 | `--top 144,146,148,150=tree_blue --base 149=tree_blue --top 128,132,136=pine_blue --base 130,134,138=pine_blue` |
| 21 | `--top 216,218,223=tree_light_gray --top 219,220,222=tree_dark_gray --base 217=tree_light_gray --base 221=tree_dark_gray` |

---

## Chapter 01

一个房子 (house)、两个茅草屋、三组木桶（每组 5 个，其中一组只有一半在屏幕内）。
树：深绿、浅绿。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 房子 house | `dwelling_house_1` | 4 × 6 |
| 茅草屋 thatched hut ×2 | `thatched_hut_1` | 4 × 3, cleaned as 5 × 3 (the shadow row) |
| 木桶组 barrel group ×3 | `barrel_group_1` | 2 × 3 |
| 树 ×50 | `tree_dark_green` ×28, `tree_light_green` ×22 | 1 × 1 each |

The reference chapter for the tile rules; since the trees became obstacles
its `Chapter_01.json` is produced by the pipeline like any other, from its
own painted `ShapeMatrix` (identical to `ChapterLegacy_01.txt`):
`obstacles/obstacles_01.json` (buildings) + `tree_obstacles.py` +
`map_clean.py` + `install_chapter.py`.

Tree tiles: 40, 43 (light-green tops), 44, 47 (dark-green tops), 42 (light
base), 46 (dark base). Plain grass tile: **52**, which is what every tree tile
fills with. The tree whose top is at (11, 3) has its trunk row inside the hut
at (11, 4), so it stands on (11, 3).

## Chapter 02

六个蓝顶房子 (blue house)、一个茅草屋、两组木桶（每组 5 个）。

蓝顶房子分 5 种：

| Object | DefinitionKey | Where |
|---|---|---|
| 蓝顶房子 1 | `blue_house_1` | 最大的那栋 |
| 蓝顶房子 2 | `blue_house_2` | 出现两次：右上角和左下角 |
| 蓝顶房子 3 | `blue_house_3` | 中央 |
| 蓝顶房子 4 | `blue_house_4` | 右侧 |
| 蓝顶房子 5 | `blue_house_5` | 正下方 |
| 茅草屋 ×1 | `thatched_hut_1` | reuse from chapter 01 |
| 木桶组 ×2 | `barrel_group_1` | reuse from chapter 01 |

Resolved — the map is 27 × 21 tiles. Footprints are rows × cols:

| Id | Key | Position | Footprint | Note |
|---|---|---|---|---|
| 1 | `blue_house_1` | (2, 1) | 9 × 10 | |
| 2 | `blue_house_2` | (21, 0) | 7 × 7 | one row off the top |
| 3 | `blue_house_3` | (13, 6) | 8 × 8 | |
| 4 | `blue_house_4` | (24, 8) | 7 × 6 | two cols off the right edge — inferred |
| 5 | `blue_house_2` | (1, 12) | 7 × 7 | the fully visible instance |
| 6 | `blue_house_5` | (16, 17) | 8 × 4 | three rows off the bottom — inferred |
| 7 | `thatched_hut_1` | (21, 17) | 4 × 3 | art covers 5 rows; cleaned with `Size` 5 × 3 |
| 8 | `barrel_group_1` | (7, 13) | 2 × 3 | left column hidden behind house #5 in the 2D art |
| 9 | `barrel_group_1` | (21, 16) | 2 × 3 | |

Plain grass tile: **153**. Trees (obstacles, 47): tops 81, 135 light green and
82, 138, 140 dark green; bases 136 light, 139 dark. The crowns painted along
row 1 over `blue_house_1` and row 12 over `blue_house_2` are behind those
houses and are dropped (no free row behind them).

---

## Chapter 03

没有 obstacles。整张图是地形：上下两岸的草地/泥地、中间的大河，以及横跨全图的木吊桥（桥板和红色栏杆柱都是普通 tile）。底部有一片针叶林。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved — the map is 18 × 26 tiles. `chapter_map.py verify` matches the
artwork exactly (0 / 269568 mismatched pixels), so nothing is painted over the
tile grid, and a close look at the art confirms there is no house, hut or
barrel to lift out. `Chapter_03_Cleaned.json` is therefore identical to the
painted map and `Obstacles` is empty.

Plain grass tile: **31**. Trees (obstacles, 11): tops 73, 84 light green and
72, 85 dark green; base 77 light green.

---

## Chapter 04

没有 obstacles。整张图是一片森林：草地、蜿蜒的泥土小路、中间一条宽的土路，
底部有两段木栅栏，图上还散着两个宝箱。房子、茅草屋、木桶都没有。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved — the map is 20 × 20 tiles. `chapter_map.py verify` matches the
artwork exactly (0 / 230400 mismatched pixels), and there is no building, hut
or barrel in the art, so `Chapter_04_Cleaned.json` is identical to the painted
map and `Obstacles` is empty. The fences (tiles 24–31) and the two treasure
chests (144, 148) stay ordinary painted tiles — they were deliberately **not**
lifted into obstacles.

树：深绿、浅绿、亮绿 -- 68 tree obstacles, `tree_dark_green` ×33,
`tree_light_green` ×22, `tree_bright_green` ×13, derived by `tree_obstacles.py`
into `obstacles/obstacles_04_with_trees.json`.

Plain grass tile: **20**, which every tree tile fills with. Tree tiles, by the
cone's core colour: 128, 130, 137, 142 dark-green tops and 129 the dark base;
131, 133, 141 light-green tops and 134 the light base; 132, 136, 138
bright-green tops and 139 the bright base. (Before the trees became obstacles
these were crowns stamped on the tiles at `--tree-stretch 1.4`; the
`Shapes_04` tree models are no longer referenced.)

---

## Chapter 05

Sera 村本身：左上角一整排红顶大教堂、右上角带十字架的蓝顶教堂、村子中间的红顶大宅、
两栋独立的蓝顶房子、三座木头仓房（和第 02 关同一张贴图）。左右两边各有一栋只露出
一列的房子。底边是白色尖木栅栏，图上还散着木柴堆、树桩和宝箱。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 红顶大教堂 red cathedral | `red_cathedral_1` + `red_cathedral_2` | 9 × 10 and 9 × 9 |
| 蓝顶教堂 blue church | `blue_church_1` + `blue_church_2` | 8 × 7 and 8 × 5 |
| 红顶大宅 red mansion | `red_mansion_1` | 9 × 10 |
| 蓝顶房子（有门） | `blue_hall_1` | 8 × 4 |
| 蓝顶房子（两扇窗） | `blue_hall_2` | 8 × 4 |
| 木头仓房 ×3 | `thatched_hut_1` | reuse from chapter 01 |

Resolved — the map is 39 × 23 tiles, the biggest so far, and about half of it is
building. `chapter_map.py verify` matches the artwork exactly (0 / 516672
mismatched pixels), so nothing is painted over the tile grid and the buildings
had to be found in the art; the `Blocked` tiles locate them almost exactly.

**Two buildings are wider than a `.vox` can be.** Coordinates are one byte each,
so no model may exceed 255 voxels — 10 tiles. The cathedral is 19 wide and the
church 12, so each is two models standing side by side, cut where the entrance
porch falls inside one piece. `build_obstacles_05.hall()` gives the roof behind
the front bays a ridge that is uniform along x, which is what lets the two
halves meet without a crease.

| Id | Key | Position | Size | Note |
|---|---|---|---|---|
| 1 | `red_cathedral_1` | (1, 1) | | tiles X 1..10 |
| 2 | `red_cathedral_2` | (11, 1) | | tiles X 11..19 |
| 3 | `blue_church_1` | (26, -1) | | two rows off the top |
| 4 | `blue_church_2` | (33, -1) | | two rows off the top |
| 5 | `red_mansion_1` | (18, 13) | | |
| 6 | `blue_hall_1` | (6, 12) | | |
| 7 | `blue_hall_2` | (34, 15) | | |
| 8 | `thatched_hut_1` | (9, 14) | 5 × 3 | |
| 9 | `thatched_hut_1` | (27, 14) | 5 × 3 | |
| 10 | `thatched_hut_1` | (38, 2) | 5 × 3 | runs off the right edge |
| 11 | `red_cathedral_2` | (-7, 13) | 6 × 9 | one column of it on the board, at the left edge |
| 12 | `red_mansion_1` | (39, 13) | 7 × 1 | one column of it on the board, at the right edge |

Ids 10–12 are buildings that show a single column of themselves at a map edge.
Each reuses a whole model placed off the board with a `Size` that clips the
cleaning back to the tiles the art covers — without that, id 12 would clear
three columns of forest that are still on screen.

The picket fence along the bottom (tiles 240–243), the log piles (156, 157), the
tree stump (218) and the chests (250) stay ordinary painted tiles, as chapter 04
left its fences.

Plain grass tile: **101**. Cobbled plaza: 100. Trees (obstacles, 70): tops
160, 167 dark green, 163, 164 blue, 168, 175 dark red, 171, 172 light red;
bases 161, 165, 169, 173 in the same order. Tiles 156, 157 are log piles, not
trees. The crowns painted over the mansion's back row (row 13) and the
cathedral's are behind those buildings and are dropped.

Cleaning needs `--fill-plain-only`: the village is ringed by forest, and without
it the conifers win the fill vote and march across the plaza the cathedral was
standing on.

---

## Chapter 06

海边的港口：上半张图是石砌的码头，堆着八个木头货箱，右边有一堆原木，靠海一侧是矮
栅栏；下半张图是草地，左下角一整栋带十字架的红顶教堂，右下角另一栋红顶教堂跑出右
边界，中间散着一片秋色的针叶林和一个宝箱。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 红顶教堂（中央大门 + 十字架） | `red_church_1` | 8 × 10 |
| 红顶教堂（右边界那栋） | `red_cathedral_2` | reuse from chapter 05 |
| 木头货箱 ×8 | `wooden_crate_1` | 3 × 2 painted, 2 × 2 on the ground |

Resolved — the map is 20 × 26 tiles. `chapter_map.py verify` matches the artwork
exactly (0 / 299520 mismatched pixels), so nothing is painted over the tile grid
and the buildings had to be found in the art.

**The right-hand church is chapter 05's cathedral standing again.** Both churches
are the same row-of-gabled-bays architecture, and `red_cathedral_2` is the piece
with no porch in it — which is exactly what the right-hand one shows, four bays
running off the edge of the board. Only seven of its nine columns are on screen,
so its `Size` clips the cleaning back to those. The left-hand one could not be
reused: its entrance is in the *middle*, under the cross, and every existing red
model puts the porch at an end, so it is built by `build_obstacles_06.py` out of
the same `housekit` parts.

**A crate is drawn over three tile rows but stands on two.** One row is lid seen
from above and two are the front face standing over the tile in front of it, so
the model is 2 × 2 and the `Size` that cleans the art is 2 × 3.

| Id | Key | Position | Size | Note |
|---|---|---|---|---|
| 1 | `red_church_1` | (1, 17) | | tiles X 1..10, three bays and the porch |
| 2 | `red_cathedral_2` | (14, 15) | 7 × 8 | two columns off the right edge |
| 3 | `wooden_crate_1` | (1, 4) | 2 × 3 | |
| 4 | `wooden_crate_1` | (6, 4) | 2 × 3 | |
| 5 | `wooden_crate_1` | (11, 5) | 2 × 3 | |
| 6 | `wooden_crate_1` | (2, 6) | 2 × 3 | |
| 7 | `wooden_crate_1` | (7, 6) | 2 × 3 | |
| 8 | `wooden_crate_1` | (1, 7) | 2 × 3 | |
| 9 | `wooden_crate_1` | (13, 8) | 2 × 3 | |
| 10 | `wooden_crate_1` | (1, 10) | 2 × 3 | |

Four of the crates overlap a neighbour by one tile and `map_clean.py` says so.
That is the art: the quay draws them stacked against each other, so the models
are left stacked too.

The log pile (tiles 261, 262, 264, 266–274), the fence along the quay (245, 255–259)
and the chest (286) stay ordinary painted tiles, as chapters 04 and 05 left theirs.

Plain grass tile: **206**. Cobbled quay: 100. Trees (obstacles, 13): tops 168,
175 dark red and 171, 172 light red; bases 169 dark red, 173 light red. The
crowns painted over the churches' back rows are dropped.

Cleaning needs `--fill-plain-only`, for the same reason chapter 05 does: the
churches stand in the trees, and without it the conifers grow across the plaza.

---

## Chapter 07

没有 obstacles。通往王都的林间大道：一整片针叶林里一条南北向的土路，只有树和草地。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- `Chapter_07_Cleaned.json` is identical to the painted map and
`Obstacles` holds only the trees (243): tops 124, 131 dark green, 127, 128
blue, 132, 139 dark red, 135, 136 light red; bases 125, 129, 133, 137 in the
same order. Tiles 140, 142 are a log pile and a stump. Three crown tiles on
row 1 at the right edge show no ground and are filled by their neighbours.

---

## Chapter 08

王城前的战场。顶部一整排城堡城墙，中间是城门，门前一座跨过护城河的木桥；桥两侧各一片
秋色圆树林，路边立着石雕像；中段两道矮石墙隔出三段路，两侧是蓝绿/秋色的针叶林；底部
路两边各一栋红顶大宅。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 城堡城墙（整段，含城门与塔楼） | `castle_wall_1` | 8 × 29 -- one obstacle, see below |
| 红顶大宅 ×2 | `red_mansion_1` | reuse from chapter 05 |
| 骑士石像 ×4 | `stone_statue_1` | 1 × 1 (painted 2 × 1) |
| 石球石柱 ×2 | `stone_pillar_1` | 1 × 1 (painted 2 × 1) -- built for chapter 09, see below |

Resolved -- the map is 29 × 45 tiles. `chapter_map.py verify` matches the
artwork exactly (0 / 751680 mismatched pixels), so everything was found in the
art; the `Blocked` tiles gave the exact outlines.

**The castle wall is one obstacle, 29 tiles wide.** That is wider than a
`.vox` part can be, so `voxlib.write_vox` now writes it as three parts under a
MagicaVoxel scene graph and `vox_batch_to_obj` exports it through `voxmesh.py`,
which merges the flat faces (14,900 quads, 1.6 MB -- the one-quad-per-voxel
exporter would have written ~70 MB). `build_obstacles_08.py` builds it: a
curtain wall over rows 1..6 with a walkway and merlons, a taller inner wall
along the back two rows, a gatehouse over X 8..22 standing forward on row 7
with two dark towers whose front bays are the gate pillars on row 8, an arched
passage over X 12..18, and the dark tower the art paints at the right edge.
Only the shell is stored in the `.vox`; `voxmesh` seals it before meshing.

Its painted outline is not a rectangle -- rows 1..6 everywhere, row 7 only
under the gatehouse and at the two corners, row 8 only the pillars -- so the
obstacle carries explicit `Clear` rectangles (`map_clean.py`) and the grass
slope and moat bank on rows 7..8 stay painted.

**The red houses are `red_mansion_1` at 10 tiles in a 12-tile slot.** The art
draws the same building with a four-tile centre gable instead of two. Rather
than build a third red model the existing one is placed flush with the road,
so the two spare columns fall on the map edges (X 1..2 and X 28..29, rows
36..43 -- `Blocked` in `ShapeMatrix`, grass in `RenderMatrix`), and all 12
painted columns are cleaned via `Clear`.

**The two pillars at (11, 19) and (19, 19) are the stone-ball pillar** (tile 11),
the same art chapter 09 paints thirty times. They were first read as a hooded
figure and built as `stone_statue_2`; that model is still in the folder but no
chapter uses it, and both are now chapter 09's `stone_pillar_1`.

**A statue is painted over two rows and stands on one.** The bust tile above
the pedestal is `Type 0` in the original for the knight (tile 8), so the model
is 1 × 1 and stands on the pedestal row; the bust row is cleaned via `Clear`.

| Id | Key | Position | Note |
|---|---|---|---|
| 1 | `castle_wall_1` | (1, 1) | Clear: rows 1..6 all; row 7 X 1, 8..22, 29; row 8 X 9..11, 19..21; row 9 X 10, 20 |
| 2 | `red_mansion_1` | (3, 35) | Clear X 1..12 × rows 35..43 |
| 3 | `red_mansion_1` | (18, 35) | Clear X 18..29 × rows 35..43 |
| 4 | `stone_statue_1` | (11, 18) | Clear (11, 17..18) |
| 5 | `stone_pillar_1` | (11, 20) | Clear (11, 19..20) |
| 6 | `stone_statue_1` | (19, 18) | Clear (19, 17..18) |
| 7 | `stone_pillar_1` | (19, 20) | Clear (19, 19..20) |
| 8 | `stone_statue_1` | (12, 30) | Clear (12, 29..30) |
| 9 | `stone_statue_1` | (18, 30) | Clear (18, 29..30) |

The bridge (tiles 212..216), the two low stone walls with their gate posts
(244..261), the moat bank (270..287) and the chests (18, 22) stay ordinary
painted tiles.

Plain grass tile: **101**. Trees (obstacles, 172): the same tile ids and
colours as chapter 05 -- tops 160, 167 dark green, 163, 164 blue, 168, 175
dark red, 171, 172 light red; bases 161, 165, 169, 173.

Cleaning needs `--fill-plain-only`: the moat (364, `Blocked`) borders the wall
footprint and would otherwise flood the cleared rows.

## Chapter 09

通往王城的大道。路两边每隔两格立着一根小石柱（1 格），分两种：上面有骑士雕塑的，
和上面只有一个石球的；地图中央是一块公告板。四角和左右两侧是绿色 / 蓝绿 / 黄绿的针
叶林，底部两侧是一大片深红、深蓝相间的松树林。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 骑士石柱 ×16 | `stone_statue_1` | reuse from chapter 08 -- 1 × 1 (painted 2 × 1) |
| 石球石柱 ×14 | `stone_pillar_1` | 1 × 1 (painted 2 × 1) |
| 公告板 | `notice_board_1` | 1 × 5 (painted 4 × 5) |

Resolved -- the map is 25 × 36 tiles. `chapter_map.py verify` matches the artwork
exactly (0 / 518400 mismatched pixels), so everything is ordinary tiles and was
found from the tile ids: the knight bust is tile 76, the ball 150, the pedestal
under both 78 (the only `Blocked` one), and the board tiles 96..114 with its
`Blocked` plinth row 108..110.

**The knight is chapter 08's statue.** Tiles 76 + 78 are pixel-identical to
chapter 08's 8 + 10, so it is `stone_statue_1` again. The ball pillar is the same
pedestal with a sphere on it, built by `build_obstacles_09.py` from chapter 08's
`pedestal()`. (Chapter 08's map also paints this ball pillar -- its tile 11 --
and left it as a 2D tile; it could now be lifted with `stone_pillar_1` too.)

**The board stands on one row.** The art draws it in elevation over four rows:
frame and dark face on 13..14, the plinth on 15, a thin foot on 16. The model is
5 cols × 1 row standing on row 15 and `Clear` takes all four rows.

`obstacles_09.json` was generated from the matrix rather than placed by hand:
every 76 / 150 has a 78 directly under it and every 78 has one of them above,
so the 30 pillars are (X, Y+1) of each bust or ball with `Clear` (X, Y, 1 × 2).

| Id | Key | Position | Note |
|---|---|---|---|
| 1 | `notice_board_1` | (11, 15) | Clear X 11..15 × rows 13..16 |
| 2..7, 12..15, 22..27 | `stone_statue_1` | see the JSON | 16 knights |
| 8..11, 16..21, 28..31 | `stone_pillar_1` | see the JSON | 14 ball pillars |

The chests (142, 146, 148) stay ordinary painted tiles.

Cleaning uses `--fill 74`: every pillar and the board stand on plain grass, and
the nearest-neighbour fill would have grown a tree tile into a pedestal's place
where a pillar stands beside the forest.

Plain grass tile: **74** (the same art as chapter 08's 101). Trees (obstacles,
130): tops 82, 95 dark green, 94 light green, 80, 93 dark red, 81, 92 blue;
bases 86 dark green, 87 light green, 84 dark red, 85 blue. Tiles 92 / 93 paint
two trees one behind the other; the tile is the one in front. (The
`Shape_9_<id>_v1..3` variant crowns from before the trees became obstacles are
no longer referenced.)

---

## Chapter 10

瀑布后的洞窟。四周是岩壁，中间两个大黑洞；沿着洞壁和路口立着三种火柱：矮的（地上一盆
火）、亮的（白热的高火柱）、略暗的（橙色的高火柱），每根火柱都有两帧动画（顶上的火苗
高低不同）；地上还散着发光的火圈，以及八个宝箱。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 矮火柱 ×32 | `fire_pillar_1` | 1 × 1 |
| 亮火柱 ×26 | `fire_pillar_2` | 1 × 1 (painted 2 × 1) |
| 略暗火柱 ×13 | `fire_pillar_3` | 1 × 1 (painted 2 × 1) |

Resolved -- the map is 31 × 45 tiles. `chapter_map.py verify` matches the artwork
exactly (0 / 803520 mismatched pixels), so every pillar is ordinary tiles and was
found from the tile ids: the bowl is tile 31; the bright column is 69 with its
flame 56 on the row above; the dim column is 68 with its flame 52 above. Every
56 / 52 has its 69 / 68 directly under it.

**Every pillar is animated: two models per key.** `fire_pillar_N.vox` is the
first frame and `fire_pillar_N_f2.vox` the second; both are built by
`build_obstacles_10.py` with the same dish and column and different flame
tongues, and ObstaclesLayer plays whatever `_f2`, `_f3`, ... it finds beside a
model at `ObstacleAnimation.FramesPerSecond`. See formats.md.

**A tall pillar is painted over two rows and stands on one** -- the flame tile is
the top of a column drawn in elevation, like chapter 08's statue busts -- so the
model is 1 × 1 on the column tile and `Clear` takes both rows.

`obstacles_10.json` was generated from the matrix: ids run row by row, 71 in
all.

| Id | Key | Position | Note |
|---|---|---|---|
| bowls | `fire_pillar_1` | every 31 | see the JSON |
| bright | `fire_pillar_2` | every 69 | Clear (X, Y-1, 1 × 2) |
| dim | `fire_pillar_3` | every 68 | Clear (X, Y-1, 1 × 2) |

The glowing floor rings (tile 72) and the chests (112) stay ordinary painted
tiles.

Cleaning uses `--fill-plain-only --fill-exclude 112`: the walls are `Blocked` and
must not grow into a cleared cell, but the chests are Plain and common enough to
pass the vote, and two bowls stand right under a chest.

No trees, no shore. Plain floor: 74..95 (a random cobble texture, so the cleared
cells take their nearest neighbour rather than one id). The shape VOXs are
generated with `--flat`: the fire oranges resolve to the palette's sand colours
and would otherwise sink the floor rings one voxel.

---

## Chapter 11

没有 obstacles。河网间的林地：蓝 / 深绿的针叶林散在草地上，中央一片深红 / 浅红的
松林，几条泥土小路，一个湖，图上散着宝箱、一段原木和一个树桩。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 35 × 45 tiles. `chapter_map.py verify` matches the artwork
exactly, and there is no building on it. `Obstacles` holds only the trees (350):
`tree_blue` ×154, `tree_dark_green` ×126, `tree_dark_red` ×37, `tree_light_red`
×33, from the recorded `tree_obstacles.py` line. Plain grass tile: **161**; the
red pines' ground matches 163 (the same plain green), and the four crown tiles
deep in the red wood (133, 135, 137, 139) show no ground and take their forest's
fill. The chests (164, 166, 168, 170) and the log / stump (142) stay painted
tiles.

## Chapter 12

没有 obstacles。峡谷里的林间小道：两侧是带黑色阴影的岩壁，壁上三个洞口，草地上是
蓝 / 深绿 / 深红 / 浅红的针叶林和几块泥地。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 28 × 50 tiles, all ordinary tiles. `Obstacles` holds only
the trees (397): `tree_dark_green` ×121, `tree_blue` ×119, `tree_dark_red` ×79,
`tree_light_red` ×78. Tile 85 -- a blue crown painted over a cliff top, `Type 1`
and so not in the first classification -- is a tree too, standing on the 89
under it. Plain grass tile: **168**. The cave mouths (109-111, 181-188) stay
painted tiles.

## Chapter 13

沙漠里的营地：九顶帐篷（五顶灰绿、四顶蓝白，同一形状只是颜色不同），中间一口原木
围起来的井，几堆原木、树桩和宝箱，四周是枯树林，左右上角各一片湖。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 灰绿帐篷 ×5 | `tent_gray_1` | 3 × 3 |
| 蓝白帐篷 ×4 | `tent_blue_1` | 3 × 3 |

Resolved -- the map is 40 × 25 tiles, all ordinary tiles, so the tents were found
from the tile ids: a gray tent is the block 240 242 244 / 246 248 250 /
252 253 254 and a blue one 224 226 228 / 230 232 234 / 236 237 238 (the middle
row is 259 233 260 where log ends are painted beside it). Both are one model in
two colourways, `build_obstacles_13.py`: a striped dome over a pleated skirt with
a door in the front, 46 voxels tall, carrying the art's colours as its own
palette.

| Id | Key | Position |
|---|---|---|
| 1..5 | `tent_gray_1` | (18, 4), (27, 5), (17, 8), (17, 14), (25, 16) |
| 6..9 | `tent_blue_1` | (24, 3), (27, 10), (15, 11), (21, 18) |

The log well (272..280), the log piles (269..271, 281..283), the stumps (239,
287) and the chests (104..118) stay painted tiles. Trees (obstacles, 277):
tops 88, 95 light gray and 91, 94 dark gray; bases 89 light, 92 dark; they fill
with 97 / 31 / 21, the tan ground they stand on.

Cleaning uses `--fill-plain-only --fill-exclude 104,106,108,110,112,118`: the
chests are Plain and several stand right beside a tent. The shape VOXs are
generated with `--flat`: the pale patches of ground and the log ends carry a
highlight that resolves to the palette's sand colour and would sink one voxel,
and the only water is the two lakes in the top corners.

## Chapter 14

没有 obstacles。林间的土路：深绿 / 浅绿的针叶林和一片片枯树（浅灰 / 深灰）散在深浅
不一的草地上，一条宽土路斜穿全图。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 40 × 40 tiles, all ordinary tiles. `Obstacles` holds only
the trees (403): `tree_light_green` ×111, `tree_dark_green` ×108,
`tree_light_gray` ×94, `tree_dark_gray` ×90.

**The lawn is a mix of shades and the forests alternate two colours cell by
cell**, so the per-tile-id colour match gave the dark and light trees two
different grass tiles and the cleaned map showed a checkerboard where every
forest stood. The dead trees' bases match tile 41 exactly, so `--fill
74,75,88,89,92,93=41` puts every tree on the light lawn on it; the trees in the
dark-green corners (104, 105, 108, 109) match 176 on their own. There is no one
plain grass tile: 174 / 175 / 176 are the untextured light / medium / dark
greens and 177 the dirt road.

## Chapter 15

没有 obstacles。大湖和它的三座木桥：湖心岛、东岸的草地和泥路、右上角的枯树林，
东侧的深绿 / 浅绿针叶林，几个宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 50 × 50 tiles, all ordinary tiles. `Obstacles` holds only
the trees (323): `tree_light_green` ×118, `tree_dark_green` ×108,
`tree_light_gray` ×53, `tree_dark_gray` ×44. The green trees' ground matches
174 and the dead trees' 175 (the plain light and medium greens); the four crown
tiles with no visible ground (98, 99, 102, 103) take their forest's fill. The
bridges (192..261) stay painted tiles, as chapter 03's did.

## Chapter 16

没有 obstacles。雪原：中央三片结冰的湖，四周是雪松林（树冠全是白的，颜色只在树冠
下面那格露出来：雪红 / 雪蓝），几条泥路和宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 40 × 40 tiles, all ordinary tiles. `Obstacles` holds only
the trees (196): `tree_snow_blue` ×103, `tree_snow_red` ×93, derived with
`--colour-from-stand` as recorded. Plain snow tile: **104**, which every tree
tile fills with.

## Chapter 17

没有 obstacles。海中的雪岛：十字形的堤道，中央一圈圈的石台，台阶中间两块灰色石板
（普通地砖，可以走），零星几棵雪松，宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 46 × 45 tiles, all ordinary tiles. The two grey slabs in
the middle (192..200) are `Type 0` stairs and stay painted. `Obstacles` holds
only the pines (24): `pine_snow`. Plain snow tiles: **102** / 103 (the two
shades the pines stand on).

## Chapter 18

没有 obstacles。深渊上的长桥：左右两侧是悬崖上的草地和石阶，中间一座跨过黑色深渊的
木桥，右边崖壁上一个洞口，蓝 / 深绿的针叶树散在两岸，宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 50 × 25 tiles, all ordinary tiles. `Obstacles` holds only
the trees (41): `tree_dark_green` ×21, `tree_blue` ×20. Plain grass tile:
**168**. The chasm (black `Blocked` tiles), the bridge (307..325) and the cave
mouth stay painted.

## Chapter 19

没有 obstacles。红蓝相间的松树林：草地、几条泥路、几块灰色乱石地，宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 25 × 40 tiles, all ordinary tiles. `Obstacles` holds only
the trees (308): `tree_blue` ×159, `tree_dark_red` ×149. The tree tiles are
`Type 3` here rather than `Type 2`. Plain grass tile: **62**; the two crown
tiles with no visible ground (75, 79) take their forest's fill.

## Chapter 20

没有 obstacles。夜里的沼泽：黑色的泥沼绕着一圈圈蓝灰色的石堤，中央小岛上三段石阶，
零星的蓝松和蓝色针叶树，宝箱。

| Object | DefinitionKey | Footprint |
|---|---|---|
| — | — | — |

Resolved -- the map is 40 × 40 tiles, all ordinary tiles. `Obstacles` holds only
the trees (51): `tree_blue` ×33, `pine_blue` ×18. Plain ground tile: **21**
(the blue-grey stone). The stairs (80..103) stay painted, as chapter 17's did.

**The swamp is sunk.** Its tiles (`Type 4`) are painted in black and five dark
browns that the shore colour tables know nothing about, so the shape VOXs are
generated with `--lower 000000=2 --lower 18140c,242018,302c24,403c30,505040=1`:
the black mire two voxels below the ground, its brown edges one. The black
outlines of the chests and the shadow lines between the stair treads share
those colours and sink with them.

## Chapter 21

高原上的石头神殿：一圈圆形的石台，四角各一座和中央两座黑色的石头神龛，石台四周和
路边立着三十多根石柱（高的三格、矮的两格），中央的魔法阵和两段石阶；四周是枯树林。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 石头神龛 ×6 | `stone_shrine_1` | 1 × 3 (painted 4 × 3) |
| 高石柱 ×12 | `stone_column_1` | 1 × 1 (painted 3 × 1) |
| 矮石柱 ×20 | `stone_column_2` | 1 × 1 (painted 2 × 1) |

Resolved -- the map is 41 × 40 tiles, all ordinary tiles, so everything was found
from the tile ids and `obstacles_21.json` was generated from the matrix.

**A shrine is painted over four rows and stands on one.** Roof (146/148/150 on
grass, 172/130/174 on stone), body (152/154/156), base (158/159/160 -- the only
`Blocked` row) and foot (161/162/163): the model is 3 cols × 1 row on the base
row and `Clear` takes all four. **The columns likewise**: a tall one is ball 85
over shaft 87 over base 89 and stands on 89 with `Clear` of three rows; a short
one is ball 85 or 90 over base 89 or 94 with `Clear` of two. Two 89s on row 1 are
tall columns whose upper rows are off the top of the map. All three models are
built by `build_obstacles_21.py` in the art's blue-grey stone ramp as their own
palette.

The magic circle (276..281), the stairs (256..267), the platform's black edges
and the chests (248, 252, 272) stay painted tiles.

Plain grass tile: **34**. Trees (obstacles, 585): tops 216, 218, 223 light gray
and 219, 220, 222 dark gray; bases 217 light, 221 dark. Cleaning needs
`--fill-plain-only --fill-exclude 248,252,272`: the column bases are `Blocked`
and the chests Plain, and both stand next to cleared tiles.

## Chapter 22

黑夜里的圆形石台：台顶一块 5 格宽的黑色石碑，台上六根托着彩色水晶球的石柱（黄 / 橙 /
绿 / 紫 / 红 / 蓝），四根矮石柱，八座骑士石像（台顶两座、台下六座），中央一个魔法阵，
四段石阶，几块黑色乱石地，宝箱；地图下半部分是黑色的深渊。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 石碑 | `stone_shrine_2` | 2 × 5 (painted 4 × 5) |
| 水晶球柱 ×6 | `orb_pillar_yellow` / `_orange` / `_green` / `_purple` / `_red` / `_blue` | 1 × 1 (painted 2 × 1) |
| 矮石柱 ×4 | `stone_column_2` | reuse from chapter 21 -- tiles 64 / 68 are chapter 21's 90 / 94 |
| 骑士石像 ×8 | `stone_statue_1` | reuse from chapter 08 -- bust 120 / 124 over pedestal 32 / 33 |

Resolved -- the map is 45 × 50 tiles, all ordinary tiles, so everything was found
from the tile ids and `obstacles_22.json` was generated by `prop_obstacles.py`
(the stacks are recorded in its `_comment`), with the monument merged in from
`obstacles_22_monument.json`.

**The monument is chapter 21's shrine five tiles wide, standing on two rows.**
Roof 146 148 172 172 150, tablet 234 154 154 154 236, base 140 159 159 159 142,
foot 143 144 144 144 145 -- the same four-row elevation as `stone_shrine_1`, but
here both the tablet and the base rows are `Blocked`, so `stone_shrine_2` is
5 cols × 2 rows on rows 15..16 (`build_obstacles_22.py`, the base slab over the
footprint and the tablet on its back half) and `Clear` takes rows 14..17 with an
explicit fill per row: grass (74) on the roof row, the platform's top edge (29)
under the tablet, cobbles (76) below.

**The orbs are the knight's pedestal with a glass ball.** Tiles 252 / 248 / 254 /
250 / 256 / 258 over pedestal 33 -- the same two-row pedestal as the statues, so
each is a 1 × 1 model on the pedestal tile, built from `build_obstacles_08`'s
`pedestal()` with the ball's three shades sampled off its own tile. Six models,
one per colour, so a chapter can place any of them; they glow softly in their own
colour (`ObstaclesLayer.GetGlow`).

Every other prop stands the same way -- the bust or ball over its pedestal or
base, standing on the lower tile. The fills are the tile each prop was painted
over: 74 (grass) under 120 / 32, 76 (cobbles) under everything on the platform;
the pixel match had to be overridden for 32, 68, 120, 124, whose art is mostly
prop and matched a black or rock tile better than its own ground.

The stairs (80..91, 240..245), the magic circle (92..99), the grass tufts on the
cobbles (100..107), the black rock patches (200..223), the chests (112, 116, 118)
and the treasure marks (206) stay painted tiles.

No trees. The shape VOXs are generated with `--flat` -- there is no water on the
map and the light stone of the stairs would otherwise be read as sand.

## Chapter 23

双魔法阵的神殿：中央石台上一座祭坛（三根石球柱围着一块平放的浅灰石板），两侧平台
各一个魔法阵和两根石球柱，四周立着高石柱（三格）和矮石柱（两格），底部四座骑士
石像和两根石球柱，三段石阶，宝箱；台外是草地和褐色的岩地。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 高石柱 ×10 | `stone_column_1` | reuse from chapter 21 -- 64 / 66 / 68 (ball, shaft, base on the cobbles) |
| 矮石柱 ×8 | `stone_column_2` | reuse from chapter 21 -- 64 / 68 on the cobbles, 304 / 308 on the dirt (304 is chapter 21's 85) |
| 石球柱 ×9 | `stone_pillar_1` | reuse from chapter 09 -- ball 126 (cobbles) / 122 (grass, chapter 09's 150) over pedestal 33 / 32 |
| 骑士石像 ×4 | `stone_statue_1` | reuse from chapter 08 -- bust 120 over pedestal 32 |

Resolved -- the map is 41 × 40 tiles, all ordinary tiles; `obstacles_23.json`
was generated by `prop_obstacles.py`. **The seven "statues" on the platforms
are stone balls**, not knights: tile 126 is the ball on cobbles and only 120,
along the bottom, is the bust -- the tile comparison against chapters 08 / 09 /
21 settled that. The knight and the ball share one pedestal art, so the four
knights and the nine balls all stand on 32 / 33.

**The altar slab stays painted.** The light grey slab in the middle of the
platform (336..338 / 342..344) is `Type 0` -- creatures walk over it, as they
do over chapter 17's slabs -- so it is not lifted into a model.

Fills are the painted ground: 76 (cobbles) under everything on the platforms, 17
(dirt) under the four short columns at the altar, 74 (grass) under the knights
and the bottom pillars -- 32 and 120 forced, their pixel match preferring a
pebble tile and the black border.

The magic circles (92..99), the stairs (80..91), the chests (116, 118), the
pebbles (317, 318) and the dirt edges stay painted tiles. No trees; `--flat`.

## Chapter 24

虚空中的小岛：褐色岩地上三根石球柱和四根矮石柱围成的祭坛，四周是黑色的虚空和四道
金色的光柱，一个宝箱。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 矮石柱 ×4 | `stone_column_2` | reuse from chapter 21 -- 64 / 68 (64 is chapter 21's 85, 68 is chapter 23's 308) |
| 石球柱 ×3 | `stone_pillar_1` | reuse from chapter 09 -- ball 69 (chapter 23's 126) over pedestal 71 (chapter 22's 33) |

Resolved -- the map is 41 × 37 tiles, all ordinary tiles; `obstacles_24.json`
was generated by `prop_obstacles.py`. Fills: 43 (dirt) under the columns, 72
(cobbles) under the pillars. The light shafts (36 / 40 / 44, `Blocked`) and the
void (42) stay painted tiles; `--flat` keeps the pale yellow of the shafts, which
the shore tables would read as sand, at ground level. No trees.

## Chapter 25

熔岩洞窟：褐色的岩地被白热的熔岩河和黑色的深渊切开，沿着岩壁和路口立着第 10 关的
三种火柱（矮盆 / 亮柱 / 暗柱）和一种更高的亮柱（画三行），地上散着发光的火圈和红 / 蓝
两种宝箱，最顶上一块岩石里插着一把剑；最底下一座青石神龛：三根石球柱围着蓝灰色的
乱石堆，两旁各一根矮石柱，左右下角还各露出一根矮石柱的顶。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 矮火柱 ×13 | `fire_pillar_1` | reuse from chapter 10 -- tile 63 is chapter 10's 31 |
| 亮火柱 ×8 | `fire_pillar_2` | reuse from chapter 10 -- flame 56 over column 61 (chapter 10's 69) |
| 暗火柱 ×11 | `fire_pillar_3` | reuse from chapter 10 -- flame 52 over column 60 (chapter 10's 68); two 60s on row 1 have their flame off the top of the map |
| 高亮火柱 ×7 | `fire_pillar_4` | **new** -- flame 56 over column segment 64 over column 61, painted 3 × 1, stands on 61 |
| 矮石柱 ×4 | `stone_column_2` | reuse from chapter 21 -- ball 112 (chapter 21's 85) over base 116 (chapter 23's 308); the two 112s on row 53 have their base off the bottom and stand on their own tile |
| 石球柱 ×3 | `stone_pillar_1` | reuse from chapter 09 -- ball 117 (chapter 23's 126) over pedestal 119 (chapter 22's 33) |
| 岩浆 ×211 | `lava_25_<tile>` (4..15, 50) | **new** -- a 1 × 1 ground cover, one voxel thick, per lava tile shape; two frames |

Resolved -- the map is 25 × 53 tiles, all ordinary tiles; `obstacles_25.json`
was generated by `prop_obstacles.py`, and `obstacles_25_with_lava.json` (what
`map_clean.py` takes) is that plus the lava covers from `cover_obstacles.py`.
`fire_pillar_4` is `fire_pillar_2` with the column one tile taller
(`build_obstacles_25.py`, 24 × 24 × 72, two frames like the others).

**The lava is a ground cover, not a cleared footprint.** The white lava is
painted across 13 tile shapes (4..15 partly rock, 50 all lava) and stays a
painted tile -- the tiles carry `"glow": 0.9` in `Shapes`, so ShapesLayer draws
them self-lit (only the bright texels: the lava and its fire fringe, not the
rock on the same tile; the fringe-only tiles 0..3 glow the same way). On top of
each lava cell lies `lava_25_<tile>`: a one-voxel sheet over exactly that tile's
lava pixels (`build_obstacles_25.py`, from the tile art), white-hot with drifting
yellow / gold veins, two frames so it shimmers at the fire pillars' rate,
self-lit with no light. `ObstaclesLayer.IsGroundCover` (keys `lava_*`) keeps a
cover at full tile size, seats it on the tile's top surface and never fades it.
Each cover's `Clear` fills its cell with its own tile id, so the RenderMatrix is
untouched -- `cover_obstacles.py` writes that.

Fills: the fire pillars and the two lone column balls carry no `Fill` and take
their neighbouring Plain floor through `--fill-plain-only --fill-exclude 62,68,70`
(the floor is the random cobble 66 / 67 / 76..95, as in chapter 10; 62 is the
floor ring and 68 / 70 the chests, all Plain and next to pillars). The column
bases 116 fill with 99 (the rock scatter they stand in) and the stone pillars
117 / 119 with 120 (the blue-grey cobbles of the shrine).

The sword in the rock (48, `Gap` -- the guardian enemy rises on it), the floor
rings (62), the chests (68 / 70), the lava (50, 0..15) and the black pits stay
painted tiles. No trees; `--flat`, for the same reason as chapter 10.

## Adding a chapter

Append a section in the same shape: the Chinese description as given, then a
table of `DefinitionKey` + footprint. Check the existing keys first — a hut
that looks like chapter 01's hut **is** `thatched_hut_1`.
