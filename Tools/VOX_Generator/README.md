# vox_generator

把一组 2D sprite（4 个方向 × 3 帧走路动画的 24×24 PNG）转换成 MagicaVoxel 的 `.vox` 三维体素模型。生成的模型是一个圆滑自然的人物立体造型，可以在 MagicaVoxel 里直接打开旋转查看。

---

## 环境要求

- Python 3.8+
- Pillow

```
pip install Pillow
```

---

## 输入文件夹格式

输入文件夹里需要放 **12 张** 24×24 的 PNG，文件名严格按下列规则：

```
Icon-<id>-01.png    Icon-<id>-02.png    Icon-<id>-03.png      # 正面 (front)，3 帧走路
Icon-<id>-04.png    Icon-<id>-05.png    Icon-<id>-06.png      # 左侧 (left) ，3 帧
Icon-<id>-07.png    Icon-<id>-08.png    Icon-<id>-09.png      # 背面 (back) ，3 帧
Icon-<id>-10.png    Icon-<id>-11.png    Icon-<id>-12.png      # 右侧 (right)，3 帧
```

`<id>` 是角色编号，可以是数字（如 `006`）也可以是任意不含 `-` 的字符串。脚本会自动从 PNG 文件名里识别 `<id>`，所以你不需要传它。

例：

```
/icons/006/
    Icon-006-01.png    Icon-006-02.png    Icon-006-03.png
    Icon-006-04.png    Icon-006-05.png    Icon-006-06.png
    Icon-006-07.png    Icon-006-08.png    Icon-006-09.png
    Icon-006-10.png    Icon-006-11.png    Icon-006-12.png
```

**关于"左侧 / 右侧 / 背面"的说明**：本工具按 *"图片实际内容"* 而不是某种约定来对应——
01-03 的脸正对镜头；04-06 是侧脸朝屏幕左；07-09 是兜帽背面（看不到脸）；10-12 是侧脸朝屏幕右。如果你的素材行排列不同，可以改 `build_frame()` 里的 `front_id / left_id / back_id / right_id` 映射。

---

## 用法

### 最常见的情况：把 3 帧 VOX 直接生成到同一个文件夹

```
python png4_to_vox.py /icons/006
```

跑完后 `/icons/006/` 里会多出三个 VOX：

```
/icons/006/Icon_006_01.vox
/icons/006/Icon_006_02.vox
/icons/006/Icon_006_03.vox
```

注意输出文件名用的是下划线 `Icon_006_01.vox`，源 PNG 用的是连字符 `Icon-006-01.png`，跟原项目的命名习惯一致。

### 指定输出文件夹

```
python png4_to_vox.py /icons/006 /some/other/folder
```

`/some/other/folder` 不存在时会自动创建。源 PNG 文件夹不会被改动。

### 只生成单帧

```
python png4_to_vox.py /icons/006 --frame 2
```

可以多次传 `--frame` 生成几个特定帧：

```
python png4_to_vox.py /icons/006 --frame 1 --frame 3
```

### 看完整 CLI 帮助

```
python png4_to_vox.py --help
```

---

## 算法简介（看一眼就懂的版本）

整个生成过程做的事：

1. **去描边**——每张输入 PNG 上的最外圈黑色描边像素被剥掉（沿剪影边缘的纯黑像素替换为相邻的内部颜色），眼睛、瞳孔这种被肉色包围的纯黑像素会保留。

2. **形状（每个 Z 切片一个椭圆）**——从正面视图读出该高度的 X 宽度，从两个侧视图读出 Y 深度，在该 Z 层填充一个数学上光滑的椭圆 `((x-cx)/a)² + ((y-cy)/b)² ≤ 1`。这样每一层横截面都是圆滑的椭圆，头小、胸宽、脚窄的渐变完全由源 PNG 的剪影决定。

3. **每个体素的颜色（winner-takes-all + 特征抑制）**——
   - 算出体素在椭圆上的外向法线，跟 4 个相机方向做点积，最大值那个视图独占该体素的颜色（不混色，特征不会糊掉）。
   - 在每个视图的该行里区分"主体色"（出现次数最多）和"特征色"（出现 ≤ 3 次且与主体色 RGB 距离 > 200）。
   - 跨视图特征匹配：如果正面在 `x=x_f` 有一个特征色，侧面在 `y=y_s` 也有相近的色，那这个特征的 3D 位置就是 `(x_f, y_s, z)`，在那里盖一个体素。
   - 其他视图分到特征色的体素改用该视图的主体色——这样侧视图不会在侧面又画一个眼睛。

4. **正面像素级覆盖**——把每个 `(x, z)` 上的"最前面那个体素"的颜色直接设成正面 PNG 在 `(x, 23-z)` 处的 RGB。这保证从正面看，VOX 跟原 PNG 完全一致（眼睛、头带、脸都在）。侧面/背面不动，仍然是上一步的圆滑 3D 处理。

5. **边缘 X 列 Y 深度收窄（防"棍子变三根"）**——单独一列、伸出身体的武器（棍 / 弓 / 矛）从正面看是 1 像素宽，但纯椭圆会把这一列的颜色铺满整层 Y 深度，导致从侧面看好像是"三根棍子排成一排"（前面那根+中间幽灵+后面那根）。所以脚本对那些 `|ex| ≥ FRINGE_THRESH`（默认 `0.80`，即椭圆边缘 20% 的列）的体素额外加约束：`|dy| ≤ FRINGE_HALF_DEPTH`（默认 `1.5`，等于 3 体素深）。武器横切面被收成一根细长条，身体大部分（中心区）不受影响。

6. **同色聚类桥接**——前面几步做完后，再扫一遍：每种颜色的体素分成 3D 连通分量，对每对距离 ≤ `BRIDGE_MAX_DIST` 曼哈顿（默认 `4`）的不同分量，沿最近端点之间的直线补一串同色"桥"体素。修的就是椭圆在某些 Z 上掐掉了一节、让一根弓断成 2-3 截的情况。只填空体素，不会覆盖其他颜色。

---

## QA 工具：渲染脚本

`render_vox.py` 把生成好的 VOX 反投影成 4 个正交视角 + 12 个 3D 透视角度，方便检查模型质量。

```
python render_vox.py 1 2 3          # 渲染 frame 1、2、3
python render_vox.py                # 默认渲染 1 和 2
```

它假设 VOX 在 `./output/Icon_006_<frame>.vox`，PNG 原图在 `./original/`——主要是开发调试用，不属于正式生成管线。生成下面这些图：

- `output/rendered_{front,back,side_lo,side_hi}_f<N>.png` —— 4 个轴向视图
- `output/perspective_grid_f<N>.png` —— 12 个角度的 3D 渲染拼图
- `output/compare_original_vs_generated_f<N>.png` —— 原 PNG vs VOX 投影对比

---

## 文件清单

```
png4_to_vox.py                  生成器主脚本（用这个）
render_vox.py                   QA 反投影渲染（可选）
original/                       icon-006 的 12 张源 PNG（示例输入）
output/                         icon-006 三帧 VOX 和渲染对比图（示例输出）
README.md                       本文件
```
