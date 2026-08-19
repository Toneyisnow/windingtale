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

## Adding a chapter

Append a section in the same shape: the Chinese description as given, then a
table of `DefinitionKey` + footprint. Check the existing keys first — a hut
that looks like chapter 01's hut **is** `thatched_hut_1`.
