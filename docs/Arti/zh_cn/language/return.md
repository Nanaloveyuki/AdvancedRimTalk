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

## 多个返回值

使用逗号返回多个值，并按位置接收：

```arti
fn mod_status(mod_id) {
    let instance = core.mod(mod_id)
    return instance.active, mod_id
}

let status, name = mod_status("example.mod")
core.emit(name + ": " + status)

status, _ = mod_status("another.mod")
```

也支持 `return (status, text)`。多个返回值按有序数组保存，可以整体接收后用索引读取；
`return result` 可以继续转交这个数组。单个返回值和不带值的 `return` 保持原有行为。

多行返回值需把左括号放在 `return` 同一行，括号内允许换行及末尾逗号：

```arti
fn result() {
    return (
        true,
        "ready",
    )
}
```

单独一行的 `return` 仍立即返回 `null`，不会读取下一行作为返回值。

解包接收的数量必须与返回数量相同，`_` 也占一个位置，但不创建变量。
首次接收使用 `let status, _ = ...`，已有变量使用 `status, _ = ...`。
详见 [assignment](assignment.md)。

`return` 的作用域限于函数，顶层代码块和普通循环使用 [`break`](break.md) 或 [`continue`](continue.md) 控制流程。
