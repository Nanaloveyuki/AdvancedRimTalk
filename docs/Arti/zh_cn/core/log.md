# log

`core.log(message)` 把消息发送到 Arti 的警告/日志回调。日志内容不进入提示词：

```arti
core.log("正在生成殖民者列表")
```

在 RimWorld 中，默认只有开发模式下才会实际显示这类调试日志。它适合临时诊断，不适合向模型输出内容。

需要输出给模型时使用 [`emit`](emit.md)；需要表达条件输出时使用 [`emit_if`](emit_if.md)。
