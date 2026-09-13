# cell

`core.cell` 把地图坐标转换为 RimWorld 的格坐标，并查询格上的对象：

```arti
let position = core.cell.at(map, 10, 20)
let things = core.cell.things(map, 10, 20)
let pawns = core.cell.pawns(map, 10, 20)
```

当地图不存在、坐标无效或超出范围时，`at` 返回 `(0, 0, 0)` 形式的默认坐标；`things` 和 `pawns` 返回空集合。

坐标使用地图内部的 `x`、`z` 整数，表示地图格位置；世界地块 ID 使用 [world](world.md) 相关入口读取。
