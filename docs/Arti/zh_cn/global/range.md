# range

`range` 生成一个整数序列，结束值不包含在序列中：

```arti
range(5)          // 0, 1, 2, 3, 4
range(2, 5)       // 2, 3, 4
range(5, 0, -1)   // 5, 4, 3, 2, 1
```

## 参数

```text
range(stop)
range(start, stop)
range(start, stop, step)
```

- 只传一个参数时，起点是 `0`。
- `step` 默认是 `1`。
- `step` 可以为负数。
- `step` 必须是非零值。

最常见的用法是配合 [`for`](../language/for.md)：

```arti
for index in range(3) {
    core.emit("第 " + index + " 项", newline: true)
}
```

生成的元素过多时会触发运行时限制。让 `range` 的范围贴合提示词实际需要即可。
