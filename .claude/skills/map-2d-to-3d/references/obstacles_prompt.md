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

---

## Chapter 01

一个房子 (house)、两个茅草屋、三组木桶（每组 5 个，其中一组只有一半在屏幕内）。

| Object | DefinitionKey | Footprint (rows × cols) |
|---|---|---|
| 房子 house | `dwelling_house_1` | 4 × 6 |
| 茅草屋 thatched hut ×2 | `thatched_hut_1` | 4 × 3 |
| 木桶组 barrel group ×3 | `barrel_group_1` | 2 × 3 |

Already finished — this is the reference chapter.

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

Plain grass tile: **153**. Tree tiles: 81, 82, 135, 138, 140 (conifers) and
136, 139 (round).

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

Plain grass tile: **31**. Tree tiles: 72, 73, 77 (conifer, ref 42) and 84, 85
(cut off by the bottom map edge, ref 47).

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

Plain grass tile: **20**. Tree tiles: 128, 130, 131, 132, 133, 136, 137, 138,
141, 142 (conifer crowns, ref 42) and 129, 134, 139 (the trunk/base half of a
two-tile-tall tree, ref 47). Every crown is stretched with
`--tree-stretch 1.4`, so chapter 04's forest stands taller than chapters 01–03.

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

Plain grass tile: **101**. Cobbled plaza: 100. Tree tiles: 160, 163, 164, 167,
168, 171, 172, 175 (conifer crowns, ref 42) and 161, 165, 169, 173 (the round
base half of a two-tile tree, ref 43), all at `--tree-stretch 1.4`. This forest
is three colours, so the crowns are tinted with the new `--tree ID:REF#RRGGBB`:
green untinted, `#2c4c6c` for the blue-green conifers, `#7a2410` and `#a04824`
for the dark and bright autumn ones.

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

Plain grass tile: **206**. Cobbled quay: 100. Tree tiles: 168, 171, 172, 175
(conifer crowns, ref 42) and 169, 173 (the round base half of a two-tile tree,
ref 43), all at `--tree-stretch 1.4`. This forest is autumn, in two shades:
`#7a2410` for the dark trees (168, 169, 175) and `#a04824` for the bright ones
(171, 172, 173).

Cleaning needs `--fill-plain-only`, for the same reason chapter 05 does: the
churches stand in the trees, and without it the conifers grow across the plaza.

---

## Adding a chapter

Append a section in the same shape: the Chinese description as given, then a
table of `DefinitionKey` + footprint. Check the existing keys first — a hut
that looks like chapter 01's hut **is** `thatched_hut_1`.
