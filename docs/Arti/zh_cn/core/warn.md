# warn

`core.diag.warn(message)` 把消息发送到诊断回调。诊断消息不进入提示词：

```arti
core.diag.warn("缺少可选上下文")
```

它与 [`log`](log.md) 的输出边界相同，属于开发诊断；需要写入提示词时使用 [`emit`](emit.md)。
