# find

```text
memory.knowledge.find(tag)
```

按标签查找公共知识对象列表。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    for item in memory.knowledge.find("trade") {
        core.emit(item.content, newline: true)
    }
}
```

返回元素字段见 [knowledge_value](../knowledge_value.md)。
