# xml_text

`core.escape.xml_text(text)` 对 XML 文本内容进行转义：

```arti
let safe = core.escape.xml_text(user_text)
core.emit("<name>" + safe + "</name>")
```

它只转义文本内容，不添加外层标签。需要构造 XML 时，仍然要自己写标签和结构。

它负责已有 XML 元素中的文本内容；完整 XML 结构由调用者组织。
