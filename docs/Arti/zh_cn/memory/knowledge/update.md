# update

```text
memory.knowledge.update(id, content?, tag?, importance?, enabled?, can_be_extracted?, can_be_matched?)
```

一次更新多个公共知识字段。成功修改至少一个字段时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.update(
        id,
        content: "新的正文",
        importance: 0.8,
        enabled: true
    )
}
```

返回 `true` 表示至少一个字段更新成功。
