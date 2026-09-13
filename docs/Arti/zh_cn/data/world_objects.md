# world_objects

`core.world_objects` 提供世界物体集合：

| 成员或函数 | 含义 |
| --- | --- |
| `all` | 所有世界物体 |
| `settlements` | 定居点 |
| `settlement_bases` | 定居点基地 |
| `destroyed_settlements` | 被摧毁定居点 |
| `sites` | 世界地点 |
| `caravans` | 商队 |
| `travelling_transporters` | 运输器 |
| `peace_talks` | 和谈 |
| `route_planner_waypoints` | 路线规划点 |
| `map_parents` | 地图父对象 |
| `count` | 世界物体数量 |
| `at(tile)` | 按行星地块读取 |
| `find(id)` | 按 ID 查找 |

```arti
let object = core.world_objects.find(42)
if object != null {
    core.emit(object.label)
}
```

世界物体的额外成员取决于 RimWorld 类型；可以使用 [`read`](read.md) 读取其公开字段或属性。

`find` 接受世界物体 ID，也可以使用对象本身。读取不到时返回 `null`。
