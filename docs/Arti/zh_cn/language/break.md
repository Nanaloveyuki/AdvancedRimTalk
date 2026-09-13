# break

`break` 立即结束最近的一层 `for` 或 `while` 循环：

```arti
for value in values {
    if value == "stop" {
        break
    }
    core.emit(value, newline: true)
}
```

`break` 的作用域限于循环内部：它结束最近的一层循环，函数和外层循环继续保持当前流程。
