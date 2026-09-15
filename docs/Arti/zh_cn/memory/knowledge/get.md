# get

```text
memory.knowledge.get(id)
```

按 ID 读取公共知识对象。找不到时返回 `null`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let item = memory.knowledge.get(id)
    if item != null {
        core.emit(item.content)
    }
}
```

返回对象字段见 [knowledge_value](../knowledge_value.md)。
