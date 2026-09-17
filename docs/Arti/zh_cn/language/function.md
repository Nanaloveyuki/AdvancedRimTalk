# function

使用 `fn` 声明函数：

```arti
fn label(name) {
    return "角色：" + name
}

core.emit(label(pawn.name))
```

函数必须有名称和参数列表。参数可以按位置或名称传入，当前没有匿名函数和默认参数形式。

## 参数

调用时必须提供准确数量的参数。支持位置参数和命名参数：

```arti
fn describe(name, mood) {
    return name + " / " + mood
}

describe("Alice", "calm")
describe(mood: "calm", name: "Alice")
```

命名参数必须匹配参数名；每个参数只能赋值一次，位置参数和命名参数混用时也遵循这一规则。

不使用的参数可命名为 `_`，允许重复，但调用方仍需按位置提供这些参数：

```arti
fn first(value, _, _) { return value }
core.emit(first("kept", "ignored", "ignored"))
```

`_` 不绑定可读取的变量，也不能用作命名实参。

参数声明、调用参数、括号表达式、数组和对象字面量内部允许换行及 `//` 注释，列表允许末尾逗号：

```arti
fn describe(
    name,
    mood,
) {
    return (
        name + " / "
        + mood
    )
}
core.emit(describe(
    name: "Alice",
    mood: "calm",
))
```

这些表达式内部的换行不结束语句。函数体和控制流花括号中的换行仍分隔语句。
括号之外也可以用反斜杠紧接换行来续行；反斜杠后不能附加空格或注释。
Arti 仍使用 `fn`、花括号和 `//`，不引入 Python 的缩进语法、`#` 注释或默认参数。

## 返回值

使用 [`return`](return.md) 返回一个值，或用逗号返回多个值：

```arti
fn fallback(value) {
    if value == null {
        return "unknown"
    }
    return value
}
```

函数走到末尾但没有 `return` 时，返回 `null`。`return` 只能出现在函数中。

## 输出

函数可以调用 [`emit`](../core/emit.md)，输出会写入当前代码块的同一个缓冲区：

```arti
fn line(value) {
    core.emit("- " + value, newline: true)
    return value
}
```

普通的函数调用表达式只计算返回值：

```arti
label(pawn.name)       // 计算返回值，不写入提示词
core.emit(label(pawn.name)) // 写入提示词
```

自定义函数使用 `name(...)` 调用。字符串函数的成员调用形式见 [method](method.md)。

## 作用域

函数可以读取声明位置可见的外层变量。函数参数和函数体内的局部变量优先于外层同名变量。函数声明在所在作用域中会预先可见，因此可以在声明前调用。

顶层函数和 `const` 可以由后续提示词片段使用。片段编辑器按列表顺序收集此前已启用片段的顶层声明，
并纳入当前片段的分析与补全；禁用片段、后续片段和其他片段的 `let` 不会提供名称。

顶层函数可以捕获同片段中已经声明的 `let`，详见 [scope](scope.md)。
作为条件时必须显式调用，例如 `if check_kiiro()`；`if check_kiiro` 传入的是函数本身，会报错。
