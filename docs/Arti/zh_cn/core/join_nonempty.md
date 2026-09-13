# join_nonempty

`core.text.join_nonempty(separator, values...)` 把非空值拼接成一段文本：

```arti
let line = core.text.join_nonempty(
    ", ",
    pawn.name,
    recipient.name,
    ctx.topic,
)
core.emit(line)
```

函数会把每项转换成文本、去掉首尾空白，并跳过 `null`、空文本和只有空白的文本。

也可以把数组作为第二个参数：

```arti
let line = core.text.join_nonempty(", ", [" Alice ", null, "Bob"])
```

结果是 `Alice, Bob`。

分隔符可以使用命名参数：

```arti
core.text.join_nonempty(separator: " / ", pawn.name, ctx.topic)
```

它与旧版 [`art.join`](../compatibility/join.md) 不同：`join_nonempty` 会跳过空项，旧函数会保留每个传入项的顺序并参与拼接。
