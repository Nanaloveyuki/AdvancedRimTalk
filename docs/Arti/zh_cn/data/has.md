# has

`core.has(target, member)` 检查一个显式命名空间成员或公开实例字段/属性是否可读：

```arti
if core.has(pawn, "ideology") {
    core.emit(pawn.ideology)
}
```

它只检查成员是否可读。对可选 Mod 或版本差异字段，先用 `has` 再用 [`read`](read.md) 可以减少错误。

不存在的目标或成员返回 `false`。
