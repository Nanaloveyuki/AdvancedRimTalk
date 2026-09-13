# world

`core.world` 提供当前 RimWorld 世界的只读数据：

| 成员 | 含义 |
| --- | --- |
| `raw` | 原始世界对象 |
| `info` | 世界信息 |
| `grid` | 世界网格 |
| `faction_manager` | 派系管理器 |
| `world_pawns` | 世界 Pawn |
| `world_objects` / `objects` | 世界物体 |
| `settlements` / `settlement_bases` | 定居点 |
| `destroyed_settlements` | 被摧毁的定居点 |
| `sites` | 世界地点 |
| `caravans` | 商队 |
| `travelling_transporters` | 运输器 |
| `peace_talks` | 和谈 |
| `route_planner_waypoints` | 路线规划点 |
| `map_parents` | 地图父对象 |
| `factions` / `pawns` | 世界派系和 Pawn |
| `components` | 世界组件 |
| `pocket_maps` | Pocket Map |
| `tile_count` | 地块数量 |
| `tile` | 当前地块 |

世界对象可能在没有加载世界或当前处于特殊游戏阶段时为空。需要筛选常用对象时，优先使用 [settlements](settlements.md)、[sites](sites.md)、[caravans](caravans.md) 或 [world_objects](world_objects.md)。
