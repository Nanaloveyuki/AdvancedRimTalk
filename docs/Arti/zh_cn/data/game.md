# game

`game` 是当前 RimWorld 游戏对象的提示词访问入口：

```arti
let current_map = game.map
if current_map != null {
    core.emit(core.value.default(current_map.name, "没有地图"))
} else {
    core.emit("没有地图")
}
```

常用成员：

| 成员 | 含义 |
| --- | --- |
| `map` / `current_map` | 当前地图 |
| `raw` | 当前游戏对象的原始只读引用 |
| `snapshot` | 当前提示词上下文快照 |
| 其他地图成员 | 在 `game` 上找不到时可读取当前地图对应成员 |

更完整、可筛选的游戏数据查询见 [core](../core/index.md)、[world](world.md) 和 [query](query.md)。`game.raw` 仍然受 Arti 的公开读取边界约束。

## `core.game`

`core.game` 是更完整的游戏命名空间：

| 成员 | 含义 |
| --- | --- |
| `raw` | 原始游戏对象 |
| `world` | 当前世界 |
| `current_map` | 当前地图 |
| `current_map_index` | 当前地图索引 |
| `current_map_id` | 当前地图 ID |
| `maps` | 已加载地图 |
| `map_count` | 地图数量 |
| `player_home_maps` | 玩家 Home 地图 |
| `any_player_home_map` | 任意玩家 Home 地图 |
| `player_has_control` | 玩家是否拥有控制权 |
| `info` | 游戏信息 |
| `rules` | 游戏规则 |
| `components` | 游戏组件 |
| `current_pawn` | 当前 Pawn |
| `recipient` | 当前接收者 |

```arti
let map_count = core.game.map_count
if core.game.player_has_control {
    core.emit("玩家拥有控制权")
}
```
