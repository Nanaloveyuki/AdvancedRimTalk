# scope

Arti 使用词法作用域。代码块、函数和模块绑定都有明确的可见范围。

## 代码块

在代码块中声明的变量只在该代码块及其子代码块中可见：

```arti
if pawn != null {
    let name = pawn.name
    core.emit(name)
}

// name 在这里不可用
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

内层作用域可以声明与外层不同的同名变量，但这样会降低可读性：

```arti
let name = "outer"
if true {
    let name = "inner"
    core.emit(name)
}
core.emit(name)
```

建议使用不同名称，只有在局部转换很短时才使用遮蔽。
