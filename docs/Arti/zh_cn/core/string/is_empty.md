# is_empty

`is_empty(value)` 判断文本转换后的长度是否为 `0`，返回布尔值。`null` 会按空文本处理。

## 语法

```arti
let a = is_empty("")
let b = core.string.is_empty(" ")
let c = "hello".is_empty()

core.emit(a + "|" + b + "|" + c)
```

输出 `true|false|false`。只有空白字符的文本仍有长度；需要忽略两端空白时，先调用 [`trim`](trim.md)。

