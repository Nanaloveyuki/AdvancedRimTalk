# faction

`core.faction` 是可调用的派系选择器：

```arti
let faction = core.faction("Player")
```

它的快捷成员包括：

- `player`
- `all`
- `hostile`

并提供 `find(value)`、`by_id(id)`、`by_name(name)`。

不传参数时使用当前默认派系解析规则。需要遍历派系集合时使用 [factions](factions.md)。
