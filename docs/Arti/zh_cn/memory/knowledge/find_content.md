# find_content

```text
memory.knowledge.find_content(content)
```

按正文内容查找公共知识对象列表。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let items = memory.knowledge.find_content("药品")
}
```

返回元素字段见 [knowledge_value](../knowledge_value.md)。
