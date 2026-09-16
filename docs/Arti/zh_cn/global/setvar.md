# setvar

`setvar(key, value)` 将值转换成文本并写入 RimTalk 会话变量，返回写入的文本：

必须提供名称和内容两个参数，例如 `setvar("language", language)`。`setvar(language)` 不是导出局部变量的语法；同一代码块内的控制流变量无需通过 `setvar` 传出。

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
