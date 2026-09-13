# let

`let` 声明一个可以重新赋值的局部变量：

```arti
let name = pawn.name
let count = 0
count += 1
```

声明时必须提供初始值。变量的作用域从声明位置开始，到当前代码块结束。

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

同一作用域中每个名称只能声明一次。需要不可重新赋值的绑定时，使用 [`const`](const.md)。
