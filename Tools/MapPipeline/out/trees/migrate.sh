set -e
run() { # nn, clean flags, tree specs...
  nn=$1; flags=$2; shift 2
  echo "=================== chapter $nn"
  python tree_obstacles.py $nn "$@" --with obstacles/obstacles_$nn.json -o obstacles/obstacles_${nn}_with_trees.json | grep -v "^  tile\|^  *[0-9]* |"
  python map_clean.py $nn --obstacles obstacles/obstacles_${nn}_with_trees.json $flags | grep "^fill\|^distinct\|!!\|^wrote"
  python install_chapter.py $nn | grep -v "^  ShapeMatrix\|^  RenderMatrix"
}
run 02 "" --top 81,135=tree_light_green --top 82,138,140=tree_dark_green --base 136=tree_light_green --base 139=tree_dark_green
run 03 "" --top 73,84=tree_light_green --top 72,85=tree_dark_green --base 77=tree_light_green
run 05 "--fill-plain-only" --top 160,167=tree_dark_green --top 163,164=tree_blue --top 168,175=tree_dark_red --top 171,172=tree_light_red --base 161=tree_dark_green --base 165=tree_blue --base 169=tree_dark_red --base 173=tree_light_red
run 06 "--fill-plain-only" --top 168,175=tree_dark_red --top 171,172=tree_light_red --base 169=tree_dark_red --base 173=tree_light_red
run 07 "" --top 124,131=tree_dark_green --top 127,128=tree_blue --top 132,139=tree_dark_red --top 135,136=tree_light_red --base 125=tree_dark_green --base 129=tree_blue --base 133=tree_dark_red --base 137=tree_light_red
run 08 "--fill-plain-only" --top 160,167=tree_dark_green --top 163,164=tree_blue --top 168,175=tree_dark_red --top 171,172=tree_light_red --base 161=tree_dark_green --base 165=tree_blue --base 169=tree_dark_red --base 173=tree_light_red
run 09 "--fill 74" --top 82,95=tree_dark_green --top 94=tree_light_green --top 80,93=tree_dark_red --top 81,92=tree_blue --base 86=tree_dark_green --base 87=tree_light_green --base 84=tree_dark_red --base 85=tree_blue
