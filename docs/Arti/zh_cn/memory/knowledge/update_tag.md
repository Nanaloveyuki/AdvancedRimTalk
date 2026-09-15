# update_tag

```text
memory.knowledge.update_tag(id, tag)
```

更新指定公共知识的标签。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.update_tag(id, "trade")
}
```

返回 `true` 表示标签更新成功。
