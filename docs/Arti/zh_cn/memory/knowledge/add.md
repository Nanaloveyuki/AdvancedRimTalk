# add

```text
memory.knowledge.add(tag, content, importance: 0.5)
```

新增一条公共知识并返回 ID。失败时返回空文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let id = memory.knowledge.add("trade", "殖民地偏好长期贸易。", importance: 0.7)
}
```

返回 ID 可用于 [get](get.md)、[update](update.md) 和 [remove](remove.md)。
