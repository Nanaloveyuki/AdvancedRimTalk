# return

`return` 从函数返回：

```arti
fn display_name(value) {
    if value == null {
        return "unknown"
    }
    return value
}
```

可以返回任意当前可处理的值，也可以不带值：

```arti
fn do_nothing() {
    return
}
```

不带值的 `return` 和走到函数末尾一样，结果是 `null`。

`return` 的作用域限于函数，顶层代码块和普通循环使用 [`break`](break.md) 或 [`continue`](continue.md) 控制流程。
