# caravans

`core.caravans` 提供世界商队：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有商队 |
| `player` | 玩家商队 |
| `count` | 商队数量 |
| `find(value)` | 按对象、ID 或名称查找 |

```arti
for caravan in core.caravans.player {
    core.emit(caravan.label, newline: true)
}
```
