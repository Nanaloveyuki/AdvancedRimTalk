# disable

```text
memory.knowledge.disable(id)
```

禁用指定公共知识。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.disable(id)
}
```

返回 `true` 表示禁用成功。
