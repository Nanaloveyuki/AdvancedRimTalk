# pawns

`core.pawns` 提供 Pawn 集合和筛选：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有可见 Pawn |
| `alive` | 存活 Pawn |
| `dead` | 已死亡 Pawn |
| `colonists` | 殖民者 |
| `humanlike` | 人形 Pawn |
| `animals` | 动物 |
| `prisoners` | 囚犯 |
| `slaves` | 奴隶 |
| `mechs` | 机械体 |
| `map` | 当前地图上的 Pawn |
| `world` | 世界 Pawn |
| `context` | 当前 Prompt 上下文中的 Pawn |
| `count` | 集合数量 |
| `find(value)` | 按对象、ID 或名称查找 |
| `by_id(id)` | 按 Thing ID 查找 |
| `by_name(name)` | 按名称查找 |

```arti
for colonist in core.pawns.colonists {
    core.emit("- " + colonist.name, newline: true)
}
```

`core.pawn` 是可调用的单 Pawn 选择器，并提供 `current`、`recipient`、`all` 以及同样的查找函数，见 [pawn](pawn.md)。

```arti
let current = core.pawn()
let named = core.pawn.by_name("Alice")
```

`core.pawn(value)` 会按对象、Thing ID 或名称解析一个 Pawn；不传参数时使用当前 Pawn。

字符串可以匹配 Thing ID、显示名称或 Pawn 名称；整数按 `thingIDNumber` 匹配。
