# substring

`substring(value, start, length?)` 从 `value` 截取文本。`start` 从 `0` 开始；省略 `length` 时截取到文本末尾。

## 语法

```arti
let a = substring("RimWorld", 0, 3)
let b = core.string.substring("RimWorld", 4)
let c = "RimWorld".substring(start: 3, length: 5)

core.emit(a + "|" + b + "|" + c)
```

输出 `Rim|orld|World`。

`start` 必须位于文本范围内，`length` 不能超出剩余文本长度。参数越界会产生运行时错误。
