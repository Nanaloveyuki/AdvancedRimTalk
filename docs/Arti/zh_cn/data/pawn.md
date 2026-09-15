# pawn

`pawn` 表示当前对话 Pawn。`recipient` 表示对话接收者；在没有接收者或当前请求不是双人对话时，它可能是 `null`。

```arti
let name = "未知角色"
if pawn != null {
    name = core.value.default(pawn.name, "未知角色")
}
core.emit(name)
```

## 常用成员

| 成员 | 含义 |
| --- | --- |
| `name` / `label` | Pawn 的显示名称 |
| `label_short` | 简短显示名称 |
| `name_raw` | 原始名称文本 |
| `faction` | 派系名称 |
| `faction_object` | 派系对象 |
| `id` / `thing_id_number` | Thing 数字 ID |
| `thing_id` | Thing ID 文本 |
| `def` / `race_def` | 种族 Def |
| `kind_def` | PawnKind Def |
| `map` | 所在地图 |
| `position` | 当前格坐标 |
| `position_held` | 被携带时的坐标信息 |
| `gender` | 性别 |
| `age` | 生物学年龄，单位为年 |
| `title` | 当前称谓 |
| `job` / `activity` | 当前工作或活动 |
| `mood` | 当前心情 |
| `health` | 健康状况文本 |
| `traits` | 特性文本 |
| `skills` | 技能文本 |
| `thoughts` / `fullthought` | 思想上下文文本 |
| `relations` / `social` | 关系摘要 |
| `fullsocial` | 完整社交关系文本 |
| `fullrelation` | 完整关系文本 |
| `fullinteraction` | 交互记录文本 |
| `equipment` | 装备上下文文本 |
| `ideology` | 意识形态上下文文本 |
| `genes` / `notable_genes` | 基因上下文文本 |
| `backstory` | 背景故事文本 |
| `profile` / `context` | RimTalk Pawn 上下文文本 |
| `captive_status` | 囚禁状态 |
| `location` | 位置描述 |
| `beauty` | 美貌值 |
| `cleanliness` | 清洁度 |
| `terrain` | 所在地形标签 |
| `surroundings` | 周边环境文本 |
| `nearby_things` / `nearby_things_text` | 周边环境文本 |
| `nearby_things_raw` | 周边 Thing 列表 |
| `nearby_items` | 周边物品列表 |
| `nearby_buildings` | 周边建筑列表 |
| `nearby_plants` | 周边植物列表 |
| `nearby_animals` | 周边动物列表 |
| `nearby_filth` | 周边污物列表 |

`race` 在 Biotech 可用并且存在异种型时优先返回异种型标签，否则返回种族 Def 标签。

## 结构化 Pawn 信息

`pawn.info` 提供稳定的只读结构化信息；`core.pawn.info` 是当前 Pawn 的同一入口。对任意 Pawn 都可以使用相同的写法，例如：

```arti
if pawn != null {
    let age = pawn.info.age.biological
    let health = pawn.info.health
    core.emit(
        pawn.info.name + ": " + age.years + "岁 " + age.quadrums + "象 "
        + age.days + "日 " + age.hours + "小时, state=" + health.state,
        newline: true
    )

    for hediff in health.hidden_hediffs {
        core.emit(hediff.def_name + " severity=" + hediff.severity, newline: true)
    }
}
```

完整字段按身份、年龄、健康、需求、提示词上下文、环境文本和 Tracker 分类，见 [pawn.info 字段参考](pawn_info.md)。

其中：

- `pawn.info.age.biological` 是生理年龄，`pawn.info.age.chronological` 是历法年龄。
- `years` 是完整年数，`quadrums` 是象（一个象为 15 天），`days` 是剩余天数，`hours` 是剩余小时数并可以带小数。
- `pawn.info.health.all_hediffs` 包含 `HediffSet` 中的全部健康项；`visible_hediffs` 和 `hidden_hediffs` 按 `Hediff.Visible` 分组。
- `pawn.info.health.summary` 是 RimTalk 原有的健康摘要，可能受 RimTalk 上下文设置影响；要读取隐藏健康项，应使用 `all_hediffs` 或 `hidden_hediffs`。

## 读取原始字段

除了上表中的提示词友好成员，宿主还允许通过公开实例字段和属性读取一部分游戏对象：

```arti
if pawn != null {
    let age = pawn.age_tracker.age_biological_years
    core.emit(age)
}
```

这里读取的是公开字段和属性，字段名称匹配会兼容大小写、下划线、连字符和空格的常见差异。`pawn.info` 是推荐的稳定结构；`raw` 和 `*_raw` 成员用于需要直接查看游戏公开对象的场景。Pawn 数据保持只读，Arti 不调用游戏方法。

## 空 Pawn

先检查对象，再读取成员：

```arti
if pawn != null {
    core.emit(pawn.name)
} else {
    core.emit("没有当前 Pawn")
}
```

也可以使用短路的 `&&`，见 [operator](../language/operator.md)。
