# knowledge_value

公共知识函数 `get`、`find`、`find_content` 和 `all` 返回知识对象。

## 字段

| 字段 | 含义 |
| --- | --- |
| `id` | 知识 ID |
| `tag` | 标签 |
| `content` | 正文 |
| `importance` | 重要度 |
| `keywords` | 关键词数组 |
| `enabled` / `is_enabled` | 是否启用 |
| `is_user_edited` | 是否由用户编辑 |
| `target_pawn_id` | 目标 Pawn ID，`-1` 表示通用 |
| `creation_tick` | 创建时间 tick |
| `original_event_text` | 原始事件文本 |
| `match_mode` | 关键词匹配模式 |
| `category` | 知识分类 |
| `tags` | Expand Memory 计算出的标签列表 |
| `can_be_extracted` | 可由系统抽取 |
| `can_be_matched` | 可参与匹配 |
| `is_rule` | 是否属于规则知识 |
| `format` | Expand Memory 导出格式文本 |

## 对象方法

| 方法 | 含义 |
| --- | --- |
| `update(...)` | 更新正文、标签、重要度、启用状态或扩展标记 |
| `enable()` / `disable()` | 设置启用状态 |
| `remove()` / `delete()` | 删除该知识 |

```arti
let item = memory.knowledge.get(id)
if item != null {
    item.update(content: item.content + "\n补充：已确认。")
}
```
