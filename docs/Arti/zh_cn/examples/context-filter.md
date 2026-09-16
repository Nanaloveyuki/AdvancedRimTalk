# 上下文过滤

只把当前对话需要的上下文写入提示词，减少无关内容：

```arti
let topic = core.value.default(ctx.topic, "")
if core.string.is_empty(topic) {
    core.emit("暂无主题。", newline: true)
} else if core.string.len(topic) > 200 {
    core.emit(core.string.substring(topic, 0, 200), newline: true)
} else {
    core.emit(topic, newline: true)
}
```

过滤条件应放在输出前，避免把调试字段混入最终 prompt。
