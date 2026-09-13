# array

使用方括号创建数组：

```arti
let names = ["Alice", "Bob", "Cleo"]
let empty = []
```

元素可以是不同类型：

```arti
let values = [pawn.name, null, 3, true]
```

## 访问元素

使用从零开始的整数索引：

```arti
let first = names[0]
```

不存在的索引可能得到 `null` 或运行时错误，具体取决于被读取的值提供器。需要稳定行为时，先检查 `count`。

## 遍历

```arti
for name in names {
    core.emit(name, newline: true)
}
```

数组可以作为 [`pick`](../core/pick.md) 的单个参数：

```arti
let selected = core.random.pick(names)
```

也可以直接传多个候选值：

```arti
let selected = core.random.pick("calm", "urgent", "curious")
```

## 常用成员

集合通常可以读取 `count`、`size`、`length`、`first` 和 `last`。空集合的 `first` 和 `last` 可能是 `null`；需要稳定输出时使用 [`default`](../core/default.md)。
