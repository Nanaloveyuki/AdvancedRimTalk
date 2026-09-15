# pawn.info

`pawn.info` 是 Pawn 的结构化信息对象。它把身份、年龄、健康、需求、RimTalk 上下文和公开 Tracker 集中到一个稳定入口。

```arti
if pawn != null {
    let age = pawn.info.age.biological
    core.emit(pawn.info.name + "：" + age.years + "岁", newline: true)
}
```

`core.pawn.info` 指向当前 Pawn 的同一类信息对象；没有当前 Pawn 时，`core.pawn.info.exists` 为 `false`。

## 顶层字段

| 字段 | 含义 |
| --- | --- |
| `exists` | 是否有 Pawn |
| `raw` | 原始 Pawn 对象，只读读取公开字段/属性 |
| `name` / `label_short` | 简短显示名称 |
| `label` | 完整显示名称 |
| `name_raw` | 原始名称对象 |
| `id` / `thing_id_number` | Thing 数字 ID |
| `thing_id` | Thing ID 文本 |
| `def` / `race_def` | 种族 Def |
| `def_name` / `def_label` | 种族 Def 标识和标签 |
| `kind_def` / `kind_def_name` / `kind_label` | PawnKind 信息 |
| `faction` / `faction_object` | 当前派系名称和派系对象 |
| `faction_def_name` / `faction_def_label` | 派系 Def 信息 |
| `host_faction` / `slave_faction` / `home_faction` | Host、Slave 和 Home 派系对象 |
| `race` / `race_def_name` / `race_def_label` | 种族或异种型显示信息 |
| `gender` / `title` | 性别和称谓 |
| `humanlike` / `animal` / `mechanoid` | 常见种族分类 |
| `colony_mech` / `mutant` / `subhuman` / `entity` | DLC 或特殊 Pawn 分类 |
| `shambler` / `ghoul` / `awoken_corpse` | 异常或特殊状态分类 |
| `colonist` / `free_colonist` / `prisoner` / `prisoner_of_colony` | 殖民者和囚犯状态 |
| `slave` / `slave_of_colony` / `player_controlled` | 奴隶和玩家控制状态 |
| `dead` / `downed` / `dead_or_downed` / `health_state` | 简要健康状态 |
| `drafted` | 是否被征召 |
| `spawned` / `spawned_or_any_parent_spawned` | 是否在地图或容器链上生成 |
| `destroyed` / `suspended` / `marked_for_discard` / `teleporting` | 生命周期状态 |
| `became_world_pawn_tick_abs` / `prev_map` | 世界 Pawn 时间和上一地图 |
| `developmental_stage` | 当前成长阶段枚举文本 |
| `mental_state` / `mental_state_def` / `mental_state_def_name` | 精神状态 |
| `in_mental_state` / `in_aggro_mental_state` | 精神状态布尔值 |
| `inspired` / `inspiration` / `inspiration_def` | 灵感状态 |
| `job` / `job_raw` / `job_def` / `job_def_name` / `job_def_label` | 当前工作 |
| `map` / `map_held` | 当前地图和持有地图 |
| `position` / `position_held` | 当前格和持有格 |
| `health_scale` | 健康规模 |
| `location` / `terrain` / `beauty` / `cleanliness` / `surroundings` | RimTalk 环境文本 |
| `age` | 年龄对象 |
| `health` | 健康对象 |
| `needs` | 需求对象 |
| `prompt` | RimTalk Prompt 上下文对象 |
| `trackers` | 公开 Tracker 对象集合 |
| `lifecycle` | 生命周期对象 |

## 年龄

`info.age` 基于 RimWorld 年龄 tick 计算。1 小时为 2,500 tick，1 天为 60,000 tick，1 象为 900,000 tick，1 年为 3,600,000 tick。

| 字段 | 含义 |
| --- | --- |
| `exists` / `raw` | 是否有 `Pawn_AgeTracker` 以及原始 Tracker |
| `biological` / `bio` | 生理年龄分解 |
| `chronological` / `chrono` | 历法年龄分解 |
| `biological_ticks` / `chronological_ticks` | 原始 tick |
| `biological_years` / `chronological_years` | RimWorld 年龄整数年 |
| `biological_years_float` / `chronological_years_float` | 浮点年数 |
| `biological_quadrums` / `biological_days` / `biological_hours` | 生理年龄快捷字段 |
| `chronological_quadrums` / `chronological_days` / `chronological_hours` | 历法年龄快捷字段 |
| `number` / `number_string` | RimWorld 年龄显示文本 |
| `tooltip` | RimWorld 年龄 Tooltip 文本 |
| `birth_year` / `birth_quadrum` | 出生年份和出生象 |
| `birth_day_of_quadrum` / `birth_day_of_year` | 从 1 开始的出生日 |
| `birth_day_of_quadrum_zero_based` / `birth_day_of_year_zero_based` | RimWorld Tracker 的 0 起始出生日 |
| `life_stage` / `life_stage_def` | 生命阶段信息和原始 Def |
| `life_stage_index` / `cur_life_stage_index` | 生命阶段序号 |
| `growth` / `growth_tier` / `percent_to_next_growth_tier` / `at_max_growth_tier` | 成长进度 |
| `adult` / `adult_min_age` / `adult_min_age_ticks` | 成年状态和阈值 |
| `biological_ticks_per_tick` | 生理年龄推进速度 |
| `adult_aging_multiplier` / `child_aging_multiplier` | 成年和儿童老化倍率 |

`biological`、`bio`、`chronological` 和 `chrono` 都包含：`ticks`、`years`、`quadrums`、`quadrum`、`days`、`hours`、`total_years`、`total_quadrums`、`total_days`、`total_hours`。

`life_stage` 包含：`exists`、`raw`、`def`、`def_name`、`label`、`adjective`、`developmental_stage`、`reproductive`、`visible`、`always_downed`、`claimable`、`body_size_factor`、`health_scale_factor`、`hunger_rate_factor`。

## 健康

| 字段 | 含义 |
| --- | --- |
| `exists` / `raw` | 是否有 `Pawn_HealthTracker` 以及原始 Tracker |
| `hediff_set` / `capacities` / `summary_health` | 健康内部对象 |
| `surgery_bills` / `immunity` | 手术账单和免疫 Tracker |
| `state` / `state_id` | `PawnHealthState` 文本和数值 |
| `mobile` / `downed` / `dead` | 移动、倒地和死亡状态 |
| `can_bleed` / `bleeding` | 是否可流血和当前是否有流血速率 |
| `in_pain_shock` | 是否处于痛休克 |
| `can_crawl` / `can_crawl_or_move` | 爬行或移动能力 |
| `lethal_damage_threshold` | 致死伤害阈值 |
| `health_scale` | Pawn 健康规模 |
| `pain_total` / `bleed_rate_total` | 总疼痛和总流血速率 |
| `hediff_count` / `visible_hediff_count` / `hidden_hediff_count` | 健康项数量 |
| `has_hidden_hediffs` | 是否有隐藏健康项 |
| `hediffs` / `all_hediffs` | 全部健康项，包含隐藏项 |
| `visible_hediffs` / `hidden_hediffs` | 按 `Hediff.Visible` 分组的健康项 |
| `raw_hediffs` | 原始 Hediff 列表 |
| `summary` / `detailed_summary` | RimTalk Normal 和 Full 健康摘要 |

## Hediff 对象

`info.health.hediffs`、`all_hediffs`、`visible_hediffs` 和 `hidden_hediffs` 的元素包含：

| 字段 | 含义 |
| --- | --- |
| `exists` / `raw` | 是否有 Hediff 以及原始对象 |
| `label` / `label_cap` / `description` | 显示文本 |
| `visible` / `hidden` | 是否可见 |
| `bleeding` / `bleed_rate` / `bleed_rate_scaled` | 流血状态和速率 |
| `pain_offset` / `pain_factor` | 疼痛影响 |
| `severity` / `severity_label` | 严重度 |
| `stage_index` / `stage` / `stage_raw` | 当前阶段 |
| `lethal` / `currently_life_threatening` | 致死性 |
| `summary_health_percent_impact` | 对整体健康百分比的影响 |
| `tend_priority` | 照料优先级 |
| `age_ticks` / `age_days` / `tick_added` | 持续时间和加入时间 |
| `def` / `def_info` / `def_name` / `def_label` | Hediff Def 信息 |
| `part` / `part_raw` / `part_label` / `part_def_name` | 身体部位 |
| `source` | 来源信息 |

`hediff.stage` 包含：`exists`、`raw`、`min_severity`、`label`、`override_label`、`become_visible`、`life_threatening`、`pain_factor`、`pain_offset`、`total_bleed_factor`、`natural_healing_factor`、`regeneration`、`blocks_mental_breaks`、`blocks_inspirations`、`override_mood_base`、`severity_gain_factor`、`prevent_vacuum_burns`、`blocks_sleeping`、`part_efficiency_offset`、`part_ignore_missing_hp`、`destroy_part`。

`hediff.def_info` 包含通用 Def 字段 `exists`、`raw`、`def_name`、`label`、`description`，并补充 `is_bad`、`chronic`、`tendable`、`initial_severity`、`min_severity`、`max_severity`、`lethal_severity`、`always_show_severity`、`prevents_death`、`prevents_crawling`、`prevents_pregnancy`、`prevents_lung_rot`、`is_infection`、`organic_added_bodypart`、`display_wound`、`ever_curable_by_item`、`blocks_social_interaction`、`blocks_sleeping`。

`hediff.part` 包含：`exists`、`raw`、`label`、`label_cap`、`label_short`、`index`、`def`、`def_name`、`def_label`、`custom_label`、`height`、`depth`、`coverage`、`is_core_part`、`parent`、`parent_raw`、`parent_label`、`parent_def_name`。

`hediff.source` 包含：`label`、`def`、`def_name`、`body_part_group`、`tool_label`、`hediff_def`、`hediff_def_name`。

## 需求

`info.needs` 包含：`exists`、`raw`、`all`、`misc`、`count`、`mood`、`mood_text`、`mood_level`、`mood_level_percent`、`prefers_outdoors`、`prefers_indoors`。

每个需求元素包含：`exists`、`raw`、`label`、`def`、`def_name`、`def_label`、`description`、`level`、`level_percent`、`instant_level`、`instant_level_percent`、`max_level`、`mood`。

## RimTalk Prompt 上下文

`info.prompt` 保留 RimTalk 的上下文构建结果：`context`、`race`、`genes`、`all_genes`、`ideology`、`backstory`、`traits`、`skills`、`health`、`mood`、`thoughts`、`relations`、`social`、`full_social`、`full_relation`、`full_interaction`、`equipment`、`captive_status`、`activity`、`location`、`terrain`、`beauty`、`cleanliness`、`surroundings`。

`info.prompt.full` 使用 RimTalk 的 Full 信息级别，字段名称与 `info.prompt` 相同。

## Tracker 和生命周期

`info.trackers` 暴露 Pawn 的公开 Tracker 字段：`age`、`health`、`records`、`inventory`、`melee_verbs`、`verb_tracker`、`ownership`、`carry`、`needs`、`mind_state`、`surroundings`、`thinker`、`jobs`、`stances`、`infection_vectors`、`duplicate`、`rotation`、`pather`、`natives`、`filth`、`roping`、`flight`、`equipment`、`apparel`、`skills`、`story`、`guest`、`guilt`、`royalty`、`abilities`、`ideo`、`genes`、`creep_joiner`、`work_settings`、`trader`、`style`、`style_observer`、`connections`、`training`、`caller`、`psychic_entropy`、`mutant`、`relations`、`interactions`、`player_settings`、`outfits`、`drugs`、`food_restriction`、`timetable`、`inventory_stock`、`mechanitor`、`learning`、`reading`、`drafter`、`lord`。

`info.lifecycle` 包含：`spawned`、`spawned_or_any_parent_spawned`、`destroyed`、`suspended`、`marked_for_discard`、`teleporting`、`became_world_pawn_tick_abs`、`previous_map`。

## 读取边界

`pawn.info` 用于读取数据。`raw`、`part_raw`、`stage_raw`、`raw_hediffs` 和 Tracker 成员仍遵守 Arti 的运行边界：读取公开实例字段和属性，保持提示词脚本与游戏执行逻辑分离。
