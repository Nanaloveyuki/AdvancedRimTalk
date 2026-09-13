# identifier

标识符用于命名变量、常量、函数和模块别名。

## 写法

第一个字符必须是 Unicode 字母或 `_`，后续可以使用 Unicode 字母、数字或 `_`：

```arti
let name = "Mia"
let pawn_2 = pawn
let _temporary = null
```

标识符以字母或下划线开头，名称由字母、数字和下划线组成：

```arti
let pawn-name = pawn       // `-` 会被当作减号
let 2nd = 2                // 不能以数字开头
```

语言标识符区分大小写。`name`、`Name` 和 `NAME` 是三个不同的名字。

## 命名建议

- 变量和函数使用能说明用途的短名。
- 多词名称使用下划线，例如 `pawn_context`。
- 模块别名尽量与包名接近，避免遮蔽 `core`、`ctx`、`pawn` 等内置根名称。
- 保留关键字不能作为名称，例如 `let`、`const`、`fn`、`if`、`for` 和 `return`。

模块别名还会受到内置根名称限制，详见 [use](use.md)。
