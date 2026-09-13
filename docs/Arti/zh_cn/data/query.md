# query

`core.query` 是面向使用者的查询入口。`core.data` 是它的别名。

## 查询函数

| 函数 | 含义 |
| --- | --- |
| `maps` | 地图集合 |
| `pawns(map?)` | Pawn 集合 |
| `things(map?)` | Thing 集合 |
| `buildings(map?)` | 建筑集合 |
| `plants(map?)` | 植物集合 |
| `items(map?)` | 物品集合 |
| `things_at(map, x, z)` | 指定格的 Thing |
| `pawns_at(map, x, z)` | 指定格的 Pawn |
| `objects_at(tile)` | 指定世界地块的世界物体 |
| `factions_on_map(map?)` | 地图上的派系 |
| `all_pawns` | 全部 Pawn |
| `all_factions` | 全部派系 |
| `all_mods` | 全部 Mod |

示例：

```arti
let selected_map = core.maps.current
for building in core.query.buildings(selected_map) {
    core.emit(building.label, newline: true)
}
```

没有游戏、没有地图或输入无效时，查询函数通常返回空集合。查询入口只读取数据。

## 选择器还是查询

- 需要一个 Pawn、地图、派系或 Thing：使用 [pawn](pawn.md)、[map](map.md)、[faction](faction.md) 或 [thing](thing.md)。
- 需要遍历集合：使用 `core.pawns`、`core.maps` 等集合命名空间。
- 需要跨类型读取：使用 `core.query`。
