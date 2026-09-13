# repeat

`repeat(value, count)` 重复文本 `count` 次，返回新的文本。

## 语法

```arti
let text = repeat("ha", 3)
let text2 = core.string.repeat("ha", 3)
let text3 = "ha".repeat(3)
core.emit(text)
```

结果是 `hahaha`。`count` 为 `0` 时返回空文本；负数会产生运行时错误。

