# trim_end

`trim_end(value)` 删除文本结尾的空白字符，开头内容保持原样。

## 语法

```arti
let text = trim_end("  hello  ")
let text2 = core.string.trim_end("  hello  ")
let text3 = "  hello  ".trim_end()
core.emit(text)
```

结果是 `  hello`。

