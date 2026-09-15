# exists

```text
memory.knowledge.exists(id)
```

判断指定 ID 是否存在，返回布尔值。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    if memory.knowledge.exists(id) {
        core.emit("知识存在")
    }
}
```

返回 `true` 表示指定 ID 存在。
