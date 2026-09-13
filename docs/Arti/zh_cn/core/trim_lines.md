# trim_lines

`core.text.trim_lines(text)` 清理多行文本：

- 把 CRLF 和 CR 统一成 LF；
- 去掉每行首尾空白；
- 去掉开头和结尾的空行；
- 合并连续空行。

```arti
let clean = core.text.trim_lines(raw_text)
core.emit(clean)
```

它保留行内普通字符，并把文本当作纯文本处理；Markdown、XML 和 Scriban 语义由后续流程决定。
