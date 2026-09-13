# number

Arti 支持整数、小数和指数形式：

```arti
let count = 12
let ratio = 0.75
let large = 1.2e3
let negative = -5
```

数字字面量使用十进制写法，支持整数、小数和指数形式。超出整数范围会产生词法错误。

## 运算

算术运算支持 `+`、`-`、`*`、`/` 和 `%`。数值运算会按数值类型计算：

```arti
let total = 2 + 3
let average = total / 2
```

除法的结果使用小数语义。除数为零会在运行时报告错误。

## 输出

数字写入提示词时使用稳定的、不依赖本地语言的文本格式：

```arti
core.emit("比例：" + ratio)
```

如果你需要固定格式或单位，建议自己拼接单位，并在必要时先使用 [`default`](../core/default.md) 处理空值。
