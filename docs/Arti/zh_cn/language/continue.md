# continue

`continue` 跳过当前循环迭代剩余的语句：

```arti
for value in values {
    if value == null {
        continue
    }
    core.emit(value, newline: true)
}
```

在 `for` 中它会进入下一项，在 `while` 中它会重新计算循环条件。`continue` 只能出现在循环内部。
