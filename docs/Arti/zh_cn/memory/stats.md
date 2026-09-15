# stats

`memory.knowledge.stats()` 返回公共知识库统计对象。

| 字段 | 含义 |
| --- | --- |
| `total_count` | 总条目数 |
| `enabled_count` | 已启用条目数 |
| `disabled_count` | 已禁用条目数 |
| `user_edited_count` | 用户编辑条目数 |
| `global_count` | 全局条目数 |
| `pawn_specific_count` | Pawn 专属条目数 |

```arti
let stats = memory.knowledge.stats()
core.emit("公共知识：" + stats.enabled_count + "/" + stats.total_count)
```
