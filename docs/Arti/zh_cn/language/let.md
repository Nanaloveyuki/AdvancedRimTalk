# let

`let` 声明一个可以重新赋值的局部变量。Arti 中的 `let` 语义更接近 Kotlin 的 `var`，
不是 Rust 中默认不可变的 `let`：

```arti
let name = pawn.name
let count = 0
count += 1
```

声明时必须提供初始值。只有函数创建独立的局部作用域；`if`、`for`、`while` 和普通花括号不会隔离变量。
未执行的分支不会创建变量，可用 [`exists`](../global/exists.md) 检测后读取。

## 重新赋值

```arti
let value = "before"
value = "after"
value += "!"
```

支持 `=`、`+=`、`-=`、`*=`、`/=` 和 `%=`。赋值目标必须是变量名；`pawn.name` 和 `items[0]` 适合作为读取表达式。

## 空值

可以显式初始化为 `null`：

```arti
let result = null
if result == null {
    result = "fallback"
}
```

同一语句列表中每个名称只能声明一次。不同分支或循环块中的同名 `let` 更新当前函数或代码块的变量，不创建遮蔽变量。
需要不可重新赋值的绑定时，使用 [`const`](const.md)。
