# maps

`core.maps` 提供地图集合：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有已加载地图 |
| `count` | 地图数量 |
| `current` | 当前地图 |
| `home` | 玩家 Home 地图 |
| `at(index)` / `by_index(index)` | 按列表索引读取 |
| `by_id(id)` | 按唯一 ID 读取 |
| `find(value)` | 按对象、ID 或名称解析地图 |

```arti
for current_map in core.maps.all {
    core.emit(current_map.name, newline: true)
}
```

`core.map` 是可调用的地图选择器，默认用法等价于解析一个地图：

```arti
let selected = core.map("Colony")
let current = core.map()
```

没有游戏、地图不存在或输入无效时，查询通常返回空集合或 `null`。

## 解析规则

- `null` 使用当前地图；
- 直接传入地图对象时返回该对象；
- 整数先尝试匹配唯一 ID，再尝试列表索引；
- 字符串按地图名称或唯一 ID 匹配，不区分大小写。
