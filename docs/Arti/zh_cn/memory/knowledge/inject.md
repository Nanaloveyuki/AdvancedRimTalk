# inject

```text
memory.knowledge.inject(context, max_entries: 5)
```

调用 Expand Memory 的公共知识注入逻辑，把匹配的知识合并进一段上下文文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let text = memory.knowledge.inject(ctx.prompt, max_entries: 5)
    core.emit(text, newline: true)
}
```

返回值是注入公共知识后的文本。
