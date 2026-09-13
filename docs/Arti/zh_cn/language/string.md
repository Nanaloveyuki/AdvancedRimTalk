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

## 字符串拼接

`+` 只要有一侧是字符串，就会把另一侧转换成文本后拼接：

```arti
let message = "年龄：" + pawn.age
```

`null` 转换为空文本，布尔值转换为 `true` 或 `false`，数字使用不依赖本地语言的格式。

## 空字符串

空字符串和只有空白的字符串会被 [`default`](../core/default.md)、[`coalesce`](../core/coalesce.md) 和 [`join_nonempty`](../core/join_nonempty.md) 特别处理。普通比较仍按实际值判断，空字符串与 `null` 不相等：

```arti
let a = ""
let b = null

// a == null 为 false
// core.value.default(a, "fallback") 会返回 "fallback"
```
