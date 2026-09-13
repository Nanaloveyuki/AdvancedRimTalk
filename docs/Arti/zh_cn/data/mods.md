# mods

`core.mods` 提供当前 Mod 状态：

| 成员或函数 | 含义 |
| --- | --- |
| `installed` | 已安装 Mod |
| `active` | 已启用 Mod |
| `running` | 当前运行中的 Mod |
| `all` | `installed` 的别名 |
| `installed_count` | 已安装数量 |
| `active_count` | 已启用数量 |
| `running_count` | 运行中数量 |
| `count` | 默认集合数量 |
| `dlc` | DLC 状态 |
| `find(value)` | 按包 ID、文件夹或名称查找 |

```arti
for mod in core.mods.active {
    core.emit(mod.name, newline: true)
}
```

`core.mods.dlc` 提供 `royalty_installed`、`ideology_installed`、`biotech_installed`、`anomaly_installed`、`odyssey_installed`，以及对应的 `_active` 成员。
