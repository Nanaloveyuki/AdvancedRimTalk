# things

`core.things` 主要用于读取 Thing 集合：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 全部 Thing |
| `core.things(map?)` | 指定地图上的 Thing |
| `find(value)` | 查找 Thing |
| `at(map, x, z)` | 读取坐标处的 Thing |
| `count` | Thing 数量 |

```arti
for item in core.things(map) {
    core.emit(item.label, newline: true)
}
```

`core.thing` 更适合选择一个对象或按 ID 查询，见 [thing](thing.md)。
