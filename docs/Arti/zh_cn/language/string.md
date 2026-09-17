# string

字符串可以使用单引号或双引号：

```arti
let a = "hello"
let b = 'world'
```

支持的转义序列：

| 写法 | 含义 |
| --- | --- |
| `\n` | 换行 |
| `\r` | 回车 |
| `\t` | 制表符 |
| `\0` | 空字符 |
| `\\` | 反斜杠 |
| `\"` | 双引号 |
| `\'` | 单引号 |
| `\uXXXX` | 四位十六进制 Unicode 字符 |

例如：

```arti
let quote = "他说：\"你好\""
let line = "第一行\n第二行"
let symbol = "\u2605"
```

## 多行字符串

支持三引号 `'''...'''` 和 `"""..."""`，保留内容中的换行和缩进；物理 CRLF / CR 换行统一为 LF。
为兼容旧脚本，普通单引号、双引号直接跨行的行为也保留。三引号内部可以直接写单个同类引号。

```arti
let text = """第一行
    第二行："原文"
第三行"""
core.emit(f'''玩家：{settings.player_name}
角色：{pawn.name}''')
```

字符串内的反斜杠紧接物理换行会连接两行，不保留该换行；`\n` 仍表示实际换行字符。
相邻字符串可以自动拼接，包括普通字符串与插值字符串：

```arti
let text = (
    "玩家："
    f"{settings.player_name}"
    "。"
)
```

## 字符串拼接

`+` 只要有一侧是字符串，就会把另一侧转换成文本后拼接：

```arti
let message = "年龄：" + pawn.age
```

`null` 转换为空文本，布尔值转换为 `true` 或 `false`，数字使用不依赖本地语言的格式。

## 字符串插值

在引号前加小写 `f`，用 `{...}` 插入变量或函数调用结果：

```arti
let value = "玩家"
fn greeting() { return "你好" }
core.emit(f"{greeting()}，{value}")
```

也支持单引号 `f'{value}'`。插值内容沿用 Arti 表达式，按从左到右的顺序各求值一次，
并使用与字符串拼接相同的文本转换规则。普通字符串 `"{value}"` 不会插值。

字面大括号写成 `{{` 和 `}}`：`f"{{{value}}}"` 得到 `{玩家}`。
保留普通字符串的转义规则。未声明名称、函数调用失败或大括号不匹配会正常报错。

仅支持表达式替换，不支持 Python 的 `:格式`、`!r`、`!s` 或 `{value=}` 调试形式。

## 空字符串

空字符串和只有空白的字符串会被 [`default`](../core/default.md)、[`coalesce`](../core/coalesce.md) 和 [`join_nonempty`](../core/join_nonempty.md) 特别处理。普通比较仍按实际值判断，空字符串与 `null` 不相等：

```arti
let a = ""
let b = null

// a == null 为 false
// core.value.default(a, "fallback") 会返回 "fallback"
```
