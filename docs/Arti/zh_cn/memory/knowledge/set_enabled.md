# set_enabled

```text
memory.knowledge.set_enabled(id, enabled)
```

设置指定公共知识的启用状态。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.set_enabled(id, false)
}
```

返回 `true` 表示启用状态更新成功。
