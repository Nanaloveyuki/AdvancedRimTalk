# value

`core.value` 提供空值处理函数：

- [`default`](default.md)：给一个值提供回退；
- [`coalesce`](coalesce.md)：从多个值中取第一个非空值。

```arti
let pawn_name = null
if pawn != null {
    pawn_name = pawn.name
}

let name = core.value.coalesce(pawn_name, "unknown")
```

两者都把 `null`、空白文本和空集合视为空；`0` 和 `false` 保持有效值。
