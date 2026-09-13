# append

`core.append` 是 [`core.emit`](emit.md) 的精确别名：

```arti
core.append("line", newline: true)
```

它与 `emit` 使用相同的参数、换行行为、返回值和输出缓冲区。换行完全由 `newline` 控制；`append` 只表示输出函数。
