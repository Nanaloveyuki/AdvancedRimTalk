# chat

`chat` 表示当前 RimTalk 对话历史：

```arti
core.emit(chat.history)
```

## 成员

| 成员 | 含义 |
| --- | --- |
| `history` / `chat_history` | 完整格式化的聊天历史 |
| `history_simplified` | 简化格式的聊天历史 |
| `count` / `size` | 消息数量 |
| `last` | 最后一条消息 |

聊天历史可能为空。较长历史会直接增加 Prompt 长度，使用前应考虑模型上下文窗口：

```arti
if chat.count > 0 {
    core.emit(chat.history)
}
```
