# 游戏数据

这一组页面说明 Arti 可以读取的 RimTalk 上下文和 RimWorld 数据入口。

## Prompt 上下文

- [context](context.md)：当前请求上下文。
- [pawn](pawn.md)：当前 Pawn。
- [pawn.info](pawn_info.md)：Pawn 的结构化详细信息。
- [recipient](recipient.md)：对话接收者。
- [map](map.md)：当前地图及时间环境。
- [chat](chat.md)：聊天历史。
- [game](game.md)：当前游戏。
- [current](current.md)：当前对象快捷入口。

## 世界和集合

- [world](world.md)
- [find](find.md)
- [maps](maps.md)
- [pawns](pawns.md)
- [faction](faction.md)
- [factions](factions.md)
- [settlements](settlements.md)
- [sites](sites.md)
- [caravans](caravans.md)
- [world_objects](world_objects.md)
- [mods](mods.md)

## Def、Thing 和查询

- [defs](defs.md)
- [def](def.md)
- [thing](thing.md)
- [things](things.md)
- [cell](cell.md)
- [query](query.md)
- [read](read.md)
- [has](has.md)
- [keys](keys.md)

这些入口都是只读的。找不到游戏对象或当前环境不满足条件时，优先按 `null` 或空集合处理。

对象成员读取限于宿主提供的命名空间和公开实例字段/属性。私有字段、方法调用、文件、网络、程序集和 Unity 对象的通用访问均不在接口范围内。
