# value

`memory.pawn.get`、`memory.pawn.list`、`memory.pawn.relevant` 和 `memory.pawn.retrieve` 返回记忆对象。

## 字段

| 字段 | 含义 |
| --- | --- |
| `id` | 当前记忆 ID |
| `origin_id` | 来源记忆 ID；`get` 也会按它匹配 |
| `content` | 记忆正文 |
| `type` / `type_name` | 记忆类型 |
| `layer` / `layer_name` | 所在层 |
| `game_tick` / `end_game_tick` | 记录时间范围 |
| `importance` | 重要度，通常为 `0..1` |
| `activity` | 活动度，通常为 `0..1` |
| `age` | Expand Memory 提供的年龄文本 |
| `related_pawn_id` / `related_pawn_name` | 关联 Pawn |
| `location` | 地点文本 |
| `tags` | 标签数组 |
| `keywords` | 关键词数组 |
| `is_user_edited` | 是否被用户编辑 |
| `is_pinned` | 是否固定 |
| `is_enabled` | `activity > 0` 时为真 |
| `notes` | 备注文本 |

## 对象方法

| 方法 | 含义 |
| --- | --- |
| `get()` | 返回当前对象 |
| `remove()` / `delete()` | 删除该记忆 |
| `pin()` / `unpin()` | 固定或取消固定 |
| `enable()` / `disable()` | 设置活动度为 1 或 0 |
| `set_content(content)` | 更新正文 |
| `set_importance(importance)` | 更新重要度 |
| `set_activity(activity)` | 更新活动度 |
| `set_notes(notes)` | 更新备注 |
| `add_tag(value)` / `remove_tag(value)` | 管理标签 |
| `add_keyword(value)` / `remove_keyword(value)` | 管理关键词 |
| `move(layer)` | 移动到指定层 |
| `decay(rate)` | 按比例降低活动度 |

## 示例

```arti
let item = memory.pawn.get(id)
if item != null {
    core.emit(item.content, newline: true)
    item.pin()
}
```
