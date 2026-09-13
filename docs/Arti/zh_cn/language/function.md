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

## 返回值

使用 [`return`](return.md) 返回一个值：

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
