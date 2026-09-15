# enable

```text
memory.knowledge.enable(id)
```

启用指定公共知识。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.enable(id)
}
```

返回 `true` 表示启用成功。
