# defs

`core.defs` 提供 RimWorld 的 Def 数据库只读查询：

```arti
let silver = core.defs.find("ThingDef", "Silver")
if silver != null {
    core.emit(silver.label)
}
```

## 集合和查找

| 成员或函数 | 含义 |
| --- | --- |
| `all(type: "ThingDef")` / `all_defs` | 读取指定类型的全部 Def |
| `find(type, name)` | 按 `defName`、显示名称查找 |
| `count(type: "ThingDef")` | 统计指定类型 |

`ThingDef` 是默认类型：

```arti
let things = core.defs.all()
let count = core.defs.count()
let steel = core.defs.find("ThingDef", "Steel")
```

## 类型快捷入口

以下快捷成员返回对应类型的 Def 集合：

`thing`、`pawn_kind`、`faction`、`biome`、`terrain`、`hediff`、`trait`、`skill`、`work_type`、`research`、`incident`、`recipe`、`job`、`interaction`、`thought`、`gene`、`ability`、`world_object`、`map_generator`、`game_condition`。

例如：

```arti
let humanlikes = core.defs.pawn_kind
let steel = core.defs.find("ThingDef", "Steel")
```

查找名称时会尝试匹配 `defName`、`label` 或 `LabelCap`，不区分大小写。

## 版本和 Mod 差异

Def 集合会受当前 RimWorld 版本、DLC 和 Mod 影响。查询结果为空时，用 `null` 判断并准备回退内容。
