# remove_by_tag

```text
memory.knowledge.remove_by_tag(tag)
```

删除指定标签的公共知识并返回删除数量。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let removed = memory.knowledge.remove_by_tag("temporary")
}
```

返回值是删除的条目数。
