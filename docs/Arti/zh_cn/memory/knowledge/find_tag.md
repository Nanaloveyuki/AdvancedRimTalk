# find_tag

```text
memory.knowledge.find_tag(tag)
```

`find_tag` 是按标签查找的明确写法，返回公共知识对象列表。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let items = memory.knowledge.find_tag("base")
}
```

返回元素字段见 [knowledge_value](../knowledge_value.md)。
