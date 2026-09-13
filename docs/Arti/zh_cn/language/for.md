# for

`for` 遍历一个可枚举值：

```arti
for colonist in core.pawns.colonists {
    core.emit(colonist.name, newline: true)
}
```

语法是：

```arti
for 变量 in 表达式 {
    // 循环体
}
```

数组、列表、查询结果和字符串都可以遍历。遍历字符串时，每一项是一个字符字符串。`null` 不产生任何项。

## 生成数字序列

使用 [`range`](../global/range.md)：

```arti
for index in range(1, 4) {
    core.emit(index, newline: true)
}
```

结果是 `1`、`2`、`3`。上限不包含在序列中。

## 循环控制

- [`break`](break.md)：立即结束当前循环。
- [`continue`](continue.md)：跳过本轮剩余语句，进入下一项。

```arti
for pawn_value in core.pawns.all {
    if pawn_value == null {
        continue
    }
    core.emit(pawn_value.name, newline: true)
}
```

运行时有步数限制。让循环来源和范围保持可控，避免把无界数据直接写入提示词。
