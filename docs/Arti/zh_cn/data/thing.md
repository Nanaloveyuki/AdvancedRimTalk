# thing

`core.thing` 是 Thing 选择器：

```arti
let item = core.thing("Steel")
```

它支持：

| 用法 | 含义 |
| --- | --- |
| `core.thing(value)` | 按对象、Thing ID、标签或 `defName` 查找 |
| `core.thing.find(id, map?)` | 按 Thing ID 查找 |
| `core.thing.at(map, x, z)` | 读取地图坐标处的 Thing 集合 |
| `core.thing.all` | 全部可读 Thing |
| `core.thing.count` | Thing 数量 |

地图省略时使用当前地图：

```arti
let things_here = core.thing.at(map, 10, 20)
```

查找不到时返回 `null` 或空集合。

查找字符串会匹配 Thing ID、标签或 `defName`；整数按 `thingIDNumber` 匹配。地图省略时使用当前地图。
