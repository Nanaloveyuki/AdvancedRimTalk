# trim

`trim(value)` 删除文本开头和结尾的空白字符。文本中间的空白保持原样。

## 语法

```arti
let text = trim("  hello world  ")
let text2 = core.string.trim("  hello world  ")
let text3 = "  hello world  ".trim()
core.emit(text)
```

结果是 `hello world`。

