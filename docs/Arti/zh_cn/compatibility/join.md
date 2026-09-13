# join

`art.join(separator, value, ...)` 使用第一个参数作为分隔符，拼接后面的值：

```text
{{ art.join(", ", art.pawn_name, art.recipient_name) }}
```

旧函数会保留每个传入项并参与拼接。需要跳过空项、减少多余分隔符时，使用 [`join_nonempty`](../core/join_nonempty.md)。
