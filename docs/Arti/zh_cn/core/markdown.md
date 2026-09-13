# markdown

`core.escape.markdown(text)` 对选定的 Markdown 标点添加反斜杠：

```arti
let safe = core.escape.markdown(user_text)
core.emit(safe)
```

它适合把用户文本放进已有 Markdown，处理常见标点的格式影响。完整的代码块、链接和表格语义由 Markdown 解析器处理。
