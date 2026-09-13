# json

`core.escape.json(text)` 只转义 JSON 字符串中的内容，不添加外层双引号：

```arti
let safe = core.escape.json(user_text)
core.emit("\"value\":\"" + safe + "\"")
```

它会处理双引号、反斜杠、控制字符和常见转义序列。调用者负责补上 JSON 的引号、逗号和对象结构。

构造完整 JSON 时，先明确组织每个字段，再分别转义字段值；这个函数负责字符串内容本身。
