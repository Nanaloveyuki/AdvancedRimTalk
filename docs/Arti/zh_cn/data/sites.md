# sites

`core.sites` 提供世界地点：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有地点 |
| `hostile` | 敌对地点 |
| `count` | 地点数量 |
| `find(value)` | 按对象、ID 或名称查找 |

```arti
for site in core.sites.hostile {
    core.emit(site.label, newline: true)
}
```
