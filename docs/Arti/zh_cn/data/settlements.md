# settlements

`core.settlements` 提供世界定居点：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有定居点 |
| `player` | 玩家定居点 |
| `hostile` | 敌对定居点 |
| `count` | 定居点数量 |
| `find(value)` | 按对象、ID 或名称查找 |

```arti
for settlement in core.settlements.player {
    core.emit(settlement.label, newline: true)
}
```

没有世界或没有匹配项时，集合为空。
