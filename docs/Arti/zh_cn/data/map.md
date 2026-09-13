# map

`map` 表示当前提示词上下文对应的地图。没有当前地图时可能为 `null`。

```arti
if map != null {
    core.emit("地图：" + map.name)
}
```

## 常用成员

| 成员 | 含义 |
| --- | --- |
| `time` | 当前地图的时间文本 |
| `hour` | 0 到 23 的小时 |
| `date` | 日期文本 |
| `day` | 当前 quadrum 中的日序 |
| `quadrum` | quadrum |
| `year` | 年份 |
| `season` | 季节 |
| `weather` | 天气 |
| `temperature` | 四舍五入后的室外温度文本 |
| `wealth` | 地图财富描述 |
| `events` | 地图事件文本 |
| `events_raw` | 原始事件集合 |
| `map_id` / `unique_id` | 地图唯一 ID |
| `index` | 游戏地图列表中的索引 |
| `tile` / `tile_id` | 行星地块及其 ID |
| `name` | 地图名称 |
| `pawns` | 地图上的 Pawn 集合 |
| `things` | 地图上的 Thing 集合 |
| `buildings` | 地图上的建筑集合 |

地图成员是生成时读取的值。时间、天气、温度和事件可能随下一次生成变化。
