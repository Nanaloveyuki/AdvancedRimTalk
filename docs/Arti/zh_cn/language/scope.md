# scope

Arti 使用函数作用域。`fn` 隔离局部声明，控制流和普通花括号不隔离声明。

## 代码块

执行过的 `if`、`for`、`while` 或普通代码块中的声明，在外部仍可见。
未执行的分支不会创建变量；先用 [`exists`](../global/exists.md) 判断再读取：

```arti
if pawn != null {
    let name = pawn.name
    core.emit(name)
}

if name.exists() {
    core.emit(name)
}
```

子代码块可以读取外层变量：

```arti
let prefix = "Pawn: "
if pawn != null {
    core.emit(prefix + pawn.name)
}
```

## 函数

函数参数和函数体内声明的变量属于函数作用域。函数可以读取声明位置可见的外层变量；给外层的 `let` 变量赋值也会更新外层变量。参数和函数体内的同名变量优先使用局部值。

```arti
let prefix = "Pawn: "

fn label(name) {
    return prefix + name
}
```

函数详情见 [function](function.md)。

## 代码块之间

提示词文档中的每个正式代码块拥有独立的局部作用域。要跨块保存值，请使用 [`setvar`](../global/setvar.md) 和 [`getvar`](../global/getvar.md)；保存的值是文本。

## 名字遮蔽

不同控制流块里的同名 `let` 更新同一个变量，不产生内层遮蔽：

```arti
let name = "outer"
if true {
    let name = "inner"
    core.emit(name)
}
core.emit(name)
```

以上代码输出 `innerinner`。只有函数的局部声明可以遮蔽外部名称。
同一语句列表重复声明仍报错，`const`、函数名和模块别名不能被同名 `let` 覆盖。
