# find

`core.find` 是对 RimWorld `Verse.Find` 公开静态数据的只读入口。`core.engine` 是它的兼容别名：

```arti
let map = core.find.current_map
let maps = core.find.maps
let manager = core.engine.tick_manager
```

它也提供 `game`、`current_map_index`、`map_count` 等快捷成员。

## 常用成员

`core.find` 暴露当前游戏中常见的公开对象，例如：

```text
Root, World, Maps, CurrentMap, WorldObjects, WorldPawns,
WorldGrid, FactionManager, TickManager, QuestManager,
ResearchManager, Storyteller, History, TaleManager,
PlayLog, BattleLog, LetterStack, Archive, PlaySettings,
IdeoManager, SignalManager, UniqueIDsManager, GameInfo,
Scenario, MapUI, Selector, WindowStack, ColonistBar
```

不同 RimWorld 版本或 DLC 可能使可用成员有所变化。需要兼容性更好的代码时，优先使用 [maps](maps.md)、[pawns](pawns.md) 和 [query](query.md) 中的稳定入口。

## `get`、`read` 和 `keys`

```arti
let current_map = core.find.get("CurrentMap")
let names = core.find.keys()
```

- `core.find.get(member)`：读取公开静态字段或属性；
- `core.find.read(member)`：与 `get` 相同的读取入口；
- `core.find.keys()`：列出可读的公开静态成员名。

这个入口只读取 `Find` 的公开静态成员；游戏状态写入不在 Arti 数据接口中。
