# pawn.info

`pawn.info` 是 Pawn 的稳定只读数据对象。它可以用于当前 Pawn、接收者、集合中的 Pawn 和选择器返回的 Pawn：

```arti
let target = core.pawn.current
if target != null {
    core.emit(target.info.name)
}
```

`core.pawn.info` 直接指向当前 Pawn 的信息。没有当前 Pawn 时，它仍然存在，但只有 `exists = false` 可用。

## 年龄

年龄由 RimWorld 的年龄 tick 分解得到，不依赖本地化文本。

| 路径 | 含义 |
| --- | --- |
| `info.age.biological` | 生理年龄分解 |
| `info.age.chronological` | 历法年龄分解 |
| `info.age.biological_ticks` | 生理年龄的原始 tick |
| `info.age.chronological_ticks` | 历法年龄的原始 tick |
| `info.age.number` | RimWorld 的年龄显示文本 |
| `info.age.tooltip` | RimWorld 的年龄 Tooltip 文本 |
| `info.age.life_stage` | 当前生命阶段 |
| `info.age.growth` | 当前成长进度 |
| `info.age.adult` | 是否达到成年年龄 |

`biological` 和 `chronological` 都包含以下字段：

| 字段 | 含义 |
| --- | --- |
| `ticks` | 该年龄的 tick 数 |
| `years` | 完整年数 |
| `quadrums` / `quadrum` | 完整象数，一个象为 15 天 |
| `days` | 扣除年和象后的剩余完整天数 |
| `hours` | 扣除年、象和天后的剩余小时数，可以带小数 |
| `total_years` | 以年表示的总年龄 |
| `total_quadrums` | 以象表示的总年龄 |
| `total_days` | 以天表示的总年龄 |
| `total_hours` | 以小时表示的总年龄 |

例如，`pawn.info.age.biological.years`、`pawn.info.age.biological.quadrums`、`pawn.info.age.biological.days` 和 `pawn.info.age.biological.hours` 可以直接组合成详细年龄。顶层也提供 `biological_years`、`biological_quadrums`、`biological_days`、`biological_hours` 以及对应的 `chronological_*` 快捷字段。

出生日期字段包括 `birth_year`、`birth_quadrum`、`birth_day_of_quadrum` 和 `birth_day_of_year`。不带后缀的日期从 1 开始；`*_zero_based` 保留 RimWorld Tracker 的 0 起始值。

## 健康

| 路径 | 含义 |
| --- | --- |
| `info.health.state` | `Mobile`、`Down` 或 `Dead` |
| `info.health.state_id` | `PawnHealthState` 数值 |
| `info.health.mobile` / `downed` / `dead` | 健康状态布尔值 |
| `info.health.can_bleed` | 是否具有流血条件 |
| `info.health.in_pain_shock` | 是否处于痛休克阈值 |
| `info.health.can_crawl` / `can_crawl_or_move` | 爬行或移动能力 |
| `info.health.lethal_damage_threshold` | 致死伤害阈值 |
| `info.health.pain_total` | HediffSet 的总疼痛值 |
| `info.health.bleed_rate_total` | HediffSet 的总流血速率 |
| `info.health.hediff_count` | 全部健康项数量 |
| `info.health.visible_hediff_count` | `Hediff.Visible` 为真的数量 |
| `info.health.hidden_hediff_count` | `Hediff.Visible` 为假的数量 |
| `info.health.summary` | RimTalk 普通健康摘要 |
| `info.health.detailed_summary` | RimTalk Full 健康摘要 |

健康项列表：

- `info.health.all_hediffs` / `hediffs`：全部 Hediff，包括隐藏项。
- `info.health.visible_hediffs`：`Hediff.Visible == true` 的 Hediff。
- `info.health.hidden_hediffs`：`Hediff.Visible == false` 的 Hediff。
- `info.health.raw_hediffs`：原始 `List<Hediff>`，只读访问。

每个列表元素都是结构化对象，常用字段如下：

| 路径 | 含义 |
| --- | --- |
| `hediff.label` / `label_cap` | 健康项名称 |
| `hediff.def_name` / `def_label` | Hediff Def 标识和标签 |
| `hediff.description` | 健康项描述 |
| `hediff.visible` / `hidden` | 是否可见 |
| `hediff.severity` / `severity_label` | 严重度及显示文本 |
| `hediff.bleeding` / `bleed_rate` | 流血状态及速率 |
| `hediff.pain_offset` / `pain_factor` | 疼痛影响 |
| `hediff.lethal` / `currently_life_threatening` | 致死性和当前生命威胁 |
| `hediff.stage` | 当前严重度阶段对象 |
| `hediff.part` | 身体部位对象；没有部位时 `exists = false` |
| `hediff.age_ticks` / `age_days` | 健康项存在时间 |
| `hediff.source` | 造成该健康项的来源信息 |
| `hediff.def_info` | Hediff Def 的详细信息 |

`hediff.stage` 包含 `min_severity`、`label`、`life_threatening`、`pain_factor`、`pain_offset`、`total_bleed_factor`、`natural_healing_factor`、`regeneration`、`blocks_mental_breaks`、`blocks_inspirations`、`prevents_crawling`、`prevents_pregnancy`、`prevents_lung_rot`、`blocks_sleeping` 和部位破坏相关字段。

`hediff.part` 包含 `label`、`label_cap`、`label_short`、`index`、`def_name`、`def_label`、`height`、`depth`、`coverage`、`is_core_part` 和父部位信息。

## 身份和状态

`info` 顶层提供以下稳定字段：

- 身份：`name`、`label`、`label_short`、`id`、`thing_id`、`def_name`、`def_label`、`kind_def_name`、`kind_label`。
- 阵营和种族：`faction`、`faction_def_name`、`race`、`race_def_name`、`gender`、`title`。
- 分类：`humanlike`、`animal`、`mechanoid`、`colony_mech`、`mutant`、`subhuman`、`entity`、`colonist`、`free_colonist`、`prisoner`、`slave`、`player_controlled`。
- 状态：`dead`、`downed`、`dead_or_downed`、`health_state`、`drafted`、`spawned`、`destroyed`、`suspended`、`developmental_stage`。
- 精神和工作：`mental_state`、`mental_state_def_name`、`in_mental_state`、`in_aggro_mental_state`、`inspired`、`job`、`job_def_name`、`job_def_label`。
- 位置：`map`、`map_held`、`position`、`position_held`、`health_scale`。

`info.lifecycle` 还提供 `spawned`、`spawned_or_any_parent_spawned`、`destroyed`、`suspended`、`marked_for_discard`、`teleporting`、`became_world_pawn_tick_abs` 和 `previous_map`。

## 需求、上下文和 Tracker

`info.needs` 提供 `count`、`all`、`misc`、`mood`、`mood_text`、`mood_level`、`mood_level_percent`、`prefers_outdoors` 和 `prefers_indoors`。需求元素包含 `label`、`def_name`、`def_label`、`description`、`level`、`level_percent`、`instant_level`、`instant_level_percent` 和 `max_level`。

`info.prompt` 保留 RimTalk 的上下文构建结果，包括 `context`、`race`、`genes`、`all_genes`、`ideology`、`backstory`、`traits`、`skills`、`health`、`mood`、`thoughts`、`relations`、`social`、`full_social`、`full_relation`、`full_interaction`、`equipment`、`captive_status`、`activity`、`location`、`terrain`、`beauty`、`cleanliness` 和 `surroundings`。`info.prompt.full` 使用 RimTalk 的 Full 信息级别。

`info.trackers` 集中暴露 Pawn 的公开 Tracker 字段，例如 `age`、`health`、`needs`、`mind_state`、`jobs`、`equipment`、`apparel`、`skills`、`story`、`guest`、`royalty`、`abilities`、`ideo`、`genes`、`relations`、`interactions`、`timetable`、`mechanitor`、`learning` 和 `drafter`。

这些入口只读。`raw`、`part_raw`、`stage_raw`、`raw_hediffs` 和 Tracker 成员仍然遵守 Arti 的运行边界：只能读取公开实例字段/属性，不执行任意方法。
