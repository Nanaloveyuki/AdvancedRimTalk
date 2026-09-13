# current

`core.current` 提供当前运行对象的快捷入口：

| 成员 | 含义 |
| --- | --- |
| `game` | 当前游戏 |
| `world` | 当前世界 |
| `map` / `current_map` | 当前地图 |
| `map_index` | 当前地图索引 |
| `pawn` | 当前 Pawn |
| `recipient` | 当前接收者 |
| `creating_world` | 是否正在创建世界 |
| `program_state` | 当前程序状态 |
| `root` | RimWorld 根对象 |

示例：

```arti
if core.current.map != null {
    core.emit(core.current.map.name)
}
```

它是快捷数据入口，所有成员都按读取方式访问。
