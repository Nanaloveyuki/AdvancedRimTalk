# remove

```text
memory.knowledge.remove(id)
```

删除指定公共知识，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.remove(id)
}
```

返回 `true` 表示删除成功。
