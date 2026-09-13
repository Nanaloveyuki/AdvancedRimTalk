# 语法

这是一页语法地图。每个语法功能都有自己的说明页，示例默认可以放进 `{{% ... %}}` 代码块中。

## 程序结构

```arti
use optional some.mod as some_mod

const prefix = "角色："
let name = "unknown"

fn format_name(value) {
    return prefix + value
}

if name != null {
    core.emit(format_name(name))
}
```

语句由换行或分号分隔。花括号表示代码块。函数声明会在所在作用域中预先可见，因此可以在声明前调用同一作用域中的函数。

## 词法

- [comment](comment.md)：当前只支持 `//` 行注释。
- [identifier](identifier.md)：标识符区分大小写，名称由字母、数字和下划线组成。
- [string](string.md)：支持单引号和双引号字符串。
- [number](number.md)：支持整数、小数和指数形式。
- [array](array.md)：使用 `[ ... ]` 创建数组。
- [object](object.md)：使用 `{ key: value }` 创建对象。

## 变量和作用域

- [let](let.md)：声明可重新赋值的局部变量。
- [const](const.md)：声明不可重新赋值的局部变量。
- [scope](scope.md)：了解代码块、函数和模块别名的可见范围。
- [use](use.md)：绑定一个模块。
- [group](group.md)：在顶层批量声明模块绑定。
- [function](function.md)：声明和调用自定义函数。

## 表达式

支持：

- 字面量、变量名、数组、对象；
- 成员访问：`pawn.name`；
- 索引：`items[0]`；
- 函数调用：`core.emit("text")`；
- 倒置函数调用：`"text".trim()`；
- 命名参数：`core.emit("text", newline: true)`；
- 一元运算：`!value`、`-value`；
- 二元运算：`+ - * / %`、比较、相等、`&&`、`||`、`??` 和 `|`。

函数的两种调用形式见 [function](function.md) 和 [method](method.md)；完整优先级和短路规则见 [operator](operator.md)。

## 控制流

- [if](if.md)：条件分支，也支持 `else if` 和 `else`。
- [for](for.md)：遍历可枚举值。
- [while](while.md)：按条件循环。
- [return](return.md)：从函数返回。
- [break](break.md)：结束当前循环。
- [continue](continue.md)：跳过当前循环剩余部分。

## 输出原则

Arti 把“计算”和“输出”分开：

```arti
let value = pawn.name       // 只计算
core.emit(value)                 // 才会写入提示词
```

函数调用的返回值可以继续参与计算。只有 [`emit`](../core/emit.md)、[`append`](../core/append.md) 和 [`emit_if`](../core/emit_if.md) 会写入当前代码块的输出。
