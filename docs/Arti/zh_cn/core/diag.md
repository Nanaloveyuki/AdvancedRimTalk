# diag

`core.diag` 是诊断命名空间。当前公开的诊断函数是 [`warn`](warn.md)：

```arti
core.diag.warn("这里使用了回退值")
```

诊断消息只进入诊断回调。RimWorld 中的这类日志主要用于开发模式排查；需要给模型看到的内容请使用 [`emit`](emit.md)。
