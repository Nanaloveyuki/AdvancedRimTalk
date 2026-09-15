# update_importance

```text
memory.knowledge.update_importance(id, importance)
```

更新指定公共知识的重要度。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.update_importance(id, 0.9)
}
```

返回 `true` 表示重要度更新成功。
