# factions

`core.factions` 提供派系集合：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有派系 |
| `player` | 玩家派系 |
| `hostile` | 敌对派系 |
| `non_hostile` | 非敌对派系 |
| `friendly` | 友好派系 |
| `humanlike` | 人形派系 |
| `hidden` | 隐藏派系 |
| `count` | 派系数量 |
| `find(value)` | 按对象、ID 或名称查找 |
| `by_id(id)` | 按 ID 查找 |
| `by_name(name)` | 按名称查找 |

```arti
for faction in core.factions.hostile {
    core.emit(faction.name, newline: true)
}
```

要解析一个具体派系，可以使用可调用的 [faction](faction.md)。

派系选择器可以按派系对象、load ID、名称或 `defName` 解析，不区分大小写。
