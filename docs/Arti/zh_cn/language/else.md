# else

`else` 是 [`if`](if.md) 的备用分支：

```arti
if value != null {
    core.emit(value)
} else {
    core.emit("未提供")
}
```

可以连续使用 `else if`：

```arti
if status == "ready" {
    core.emit("已准备")
} else if status == "busy" {
    core.emit("忙碌中")
} else {
    core.emit("未知状态")
}
```

`else` 必须紧跟在前一个 `if` 代码块后面。它不单独构成条件语句。
