# trim_start

`trim_start(value)` 删除文本开头的空白字符，结尾内容保持原样。

## 语法

```arti
let text = trim_start("  hello  ")
let text2 = core.string.trim_start("  hello  ")
let text3 = "  hello  ".trim_start()
core.emit(text)
```

结果是 `hello  `。

