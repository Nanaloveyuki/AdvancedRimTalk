# coalesce

`core.value.coalesce(...)` 返回参数中第一个非空值：

```arti
let pawn_name = null
if pawn != null {
    pawn_name = pawn.name
}

let recipient_name = null
if recipient != null {
    recipient_name = recipient.name
}

let name = core.value.coalesce(pawn_name, recipient_name, "unknown")
```

它使用与 [`default`](default.md) 相同的空值规则：`null`、空白文本和空集合会被跳过。

如果只传一个非字符串集合，`coalesce` 会遍历该集合并选择第一个非空项：

```arti
let value = core.value.coalesce([null, "", "first usable"])
```

没有可用值时返回 `null`。
