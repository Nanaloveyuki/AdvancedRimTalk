# setvar

`setvar(key, value)` 将值转换成文本并写入 RimTalk 会话变量，返回写入的文本：

```arti
setvar("selected_topic", core.value.default(ctx.topic, "未指定"))
```

之后可以用 [`getvar`](getvar.md) 读取：

```arti
core.emit(getvar("selected_topic"))
```

## 保存的是文本

```arti
setvar("count", 3)
let value = getvar("count")
```

`getvar` 的读取结果始终是文本。数值 `3` 会保存为文本 `"3"`，Pawn、Map、数组等对象也会以文本形式保存。

如果只在当前代码块使用数据，优先使用 [`let`](../language/let.md) 或 [`const`](../language/const.md)。
