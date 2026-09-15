# delete

```text
memory.knowledge.delete(id)
```

`delete` 是 `remove` 的别名。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.delete(id)
}
```

返回 `true` 表示删除成功。
