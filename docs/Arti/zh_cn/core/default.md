# default

`core.value.default(value, fallback)` 在第一个值为空时返回回退值，否则返回第一个值：

```arti
let topic = core.value.default(ctx.topic, "未指定")
```

以下情况视为空：

- `null`；
- 空字符串或只有空白的字符串；
- 空集合。

`0` 和 `false` 属于有效值，会原样返回。

函数参数会先求值。读取 `pawn.name` 前先用 [`if`](../language/if.md) 检查 `pawn`，再调用 `core.value.default`。
