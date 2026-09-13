# operator

## 优先级

从低到高：

| 优先级 | 运算符 |
| --- | --- |
| 1 | `|` |
| 2 | `||` |
| 3 | `&&` |
| 4 | `??` |
| 5 | `==`、`!=` |
| 6 | `<`、`<=`、`>`、`>=` |
| 7 | `+`、`-` |
| 8 | `*`、`/`、`%` |

括号可以改变计算顺序：

```arti
let value = (base + bonus) * multiplier
```

## 逻辑运算

`&&` 和 `||` 会短路，并返回布尔值：

```arti
if pawn != null && pawn.name != null {
    core.emit(pawn.name)
}
```

短路时，右侧表达式不参与求值：

```arti
let ok = value != null && value != ""
```

`??` 只在左侧为 `null` 时使用右侧，不把空字符串或空白字符串当作 `null`：

```arti
let value = ctx.topic ?? "unknown"
```

如果你也想把空白文本视为空值，使用 [`default`](../core/default.md)。

## 管道 `|`

`left | right` 会把 `left` 作为唯一的位置参数传给右侧可调用值：

```arti
fn add_prefix(value) {
    return "角色：" + value
}

let label = pawn.name | add_prefix
core.emit(label)
```

右侧必须是函数或其他可调用值。管道适合把简单的文本转换串起来；步骤较多时，使用局部变量通常更清楚。

## `+`

只要一侧是字符串，`+` 就执行文本拼接；否则执行数值加法：

```arti
let text = "count=" + 3
let total = 2 + 3
```

## 赋值

变量声明和赋值使用 `=`。还支持 `+=`、`-=`、`*=`、`/=` 和 `%=`：

```arti
let count = 1
count += 1
```

赋值目标必须是简单变量名，成员和索引用于读取：

```arti
count = 2              // 可以
pawn.name = "Alice"    // 不可以
items[0] = "Alice"     // 不可以
```

常量只允许在声明时赋值，见 [const](const.md)。
