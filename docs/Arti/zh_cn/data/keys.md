# keys

`core.keys(target)` 返回目标当前可读的成员名称：

```arti
for name in core.keys(pawn) {
    core.emit(name, newline: true)
}
```

对于懒加载命名空间，它返回该命名空间提供的键；对于普通对象，它返回公开实例字段和属性名称。

键列表可能随 RimWorld 版本、DLC 和 Mod 改变。`keys` 适合诊断和探索，不建议把顺序当作稳定 API。
