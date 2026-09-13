# if

`if` 根据条件选择一个代码块：

```arti
if pawn != null {
    core.emit("有当前 Pawn")
}
```

条件会按真值规则计算：

- `null`、`false`、数值 `0` 和空文本视为假；
- 非空文本、非零数字、对象和集合通常视为真；
- 具体数据提供器返回的空值应使用 `default` 或显式比较处理。

## `else if` 和 `else`

```arti
if pawn == null {
    core.emit("没有角色")
} else if pawn.mood == null {
    core.emit("没有心情信息")
} else {
    core.emit("心情：" + pawn.mood)
}
```

每次判断最多执行一个分支。`else` 直接承接前面的条件。

## 空值检查

访问可能不存在的成员前，先检查对象：

```arti
if recipient != null && recipient.name != null {
    core.emit(recipient.name)
}
```

`&&` 会短路，`recipient` 为空时右侧成员读取会被跳过。
