# emit

`core.emit(text, newline: false)` 把值转换成文本并写入当前 Arti 代码块的输出：

```arti
core.emit("hello")
core.emit("world", newline: true)
```

`newline` 默认为 `false`。如果省略它，下一次输出会紧接在当前文本后面。

```arti
core.emit("a")
core.emit("b", newline: true)
core.emit("c")
```

结果是：

```text
ab
c
```

## 返回值

`emit` 返回原始的 `text` 值，因此可以在赋值或函数中继续使用：

```arti
let name = core.emit(pawn.name)
```

实际使用中通常把计算和输出分开，代码会更清楚。

## 空值和其他类型

- `null` 输出为空文本；
- 布尔值输出为 `true` 或 `false`；
- 数字使用稳定的数值文本；
- 其他值使用宿主提供的文本表示。

普通表达式只求值；提示词内容由 `emit` 等输出函数写入。
