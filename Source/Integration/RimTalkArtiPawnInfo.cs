using System;
using System.Collections.Generic;
using RimTalk.Prompt;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace AdvancedRimTalk.Integration
{
    internal static class RimTalkArtiPawnInfo
    {
        public static RimTalkArtiLazyNamespace Create(Pawn pawn)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", pawn != null);
            info.Set("raw", pawn);
            if (pawn == null)
            {
                return info;
            }

            info.Set("name", () => pawn.LabelShort ?? string.Empty);
            info.Set("label", () => pawn.Label ?? string.Empty);
            info.Set("label_short", () => pawn.LabelShort ?? string.Empty);
            info.Set("name_raw", () => pawn.Name);
            info.Set("id", () => pawn.thingIDNumber);
            info.Set("thing_id_number", () => pawn.thingIDNumber);
            info.Set("thing_id", () => pawn.ThingID ?? string.Empty);
            info.Set("def", () => pawn.def);
            info.Set("def_name", () => GetDefName(pawn.def));
            info.Set("def_label", () => GetDefLabel(pawn.def));
            info.Set("kind_def", () => pawn.kindDef);
            info.Set("kind_def_name", () => GetDefName(pawn.kindDef));
            info.Set("kind_label", () => pawn.KindLabel ?? string.Empty);
            info.Set("faction", () => GetFactionName(pawn.Faction));
            info.Set("faction_object", () => pawn.Faction);
            info.Set("faction_def_name", () => pawn.Faction == null ? string.Empty : GetDefName(pawn.Faction.def));
            info.Set("faction_def_label", () => pawn.Faction == null ? string.Empty : GetDefLabel(pawn.Faction.def));
            info.Set("host_faction", () => pawn.HostFaction);
            info.Set("slave_faction", () => pawn.SlaveFaction);
            info.Set("home_faction", () => pawn.HomeFaction);
            info.Set("race", () => GetRaceLabel(pawn));
            info.Set("race_def", () => pawn.def);
            info.Set("race_def_name", () => GetDefName(pawn.def));
            info.Set("race_def_label", () => GetDefLabel(pawn.def));
            info.Set("gender", () => pawn.gender.ToString());
            info.Set("title", () => pawn.GetTitle() ?? string.Empty);

            info.Set("humanlike", () => pawn.RaceProps != null && pawn.RaceProps.Humanlike);
            info.Set("animal", () => pawn.IsAnimal);
            info.Set("mechanoid", () => pawn.RaceProps != null && pawn.RaceProps.IsMechanoid);
            info.Set("colony_mech", () => pawn.IsColonyMech);
            info.Set("mutant", () => pawn.IsMutant);
            info.Set("subhuman", () => pawn.IsSubhuman);
            info.Set("entity", () => pawn.IsEntity);
            info.Set("shambler", () => pawn.IsShambler);
            info.Set("ghoul", () => pawn.IsGhoul);
            info.Set("awoken_corpse", () => pawn.IsAwokenCorpse);
            info.Set("colonist", () => pawn.IsColonist);
            info.Set("free_colonist", () => pawn.IsFreeColonist);
            info.Set("prisoner", () => pawn.IsPrisoner);
            info.Set("prisoner_of_colony", () => pawn.IsPrisonerOfColony);
            info.Set("slave", () => pawn.IsSlave);
            info.Set("slave_of_colony", () => pawn.IsSlaveOfColony);
            info.Set("player_controlled", () => pawn.IsPlayerControlled);

            info.Set("dead", () => pawn.health != null && pawn.health.Dead);
            info.Set("downed", () => pawn.health != null && pawn.health.Downed);
            info.Set("dead_or_downed", () => pawn.health != null && (pawn.health.Dead || pawn.health.Downed));
            info.Set("health_state", () => pawn.health == null ? string.Empty : pawn.health.State.ToString());
            info.Set("drafted", () => pawn.Drafted);
            info.Set("spawned", () => pawn.Spawned);
            info.Set("spawned_or_any_parent_spawned", () => pawn.SpawnedOrAnyParentSpawned);
            info.Set("destroyed", () => pawn.Destroyed);
            info.Set("suspended", () => pawn.Suspended);
            info.Set("marked_for_discard", () => pawn.markedForDiscard);
            info.Set("teleporting", () => pawn.teleporting);
            info.Set("became_world_pawn_tick_abs", () => pawn.becameWorldPawnTickAbs);
            info.Set("prev_map", () => pawn.prevMap);
            info.Set("developmental_stage", () => pawn.DevelopmentalStage.ToString());

            info.Set("mental_state", () => GetMentalStateText(pawn));
            info.Set("mental_state_def", () => pawn.MentalStateDef);
            info.Set("mental_state_def_name", () => pawn.MentalStateDef == null ? string.Empty : GetDefName(pawn.MentalStateDef));
            info.Set("in_mental_state", () => pawn.InMentalState);
            info.Set("in_aggro_mental_state", () => pawn.InAggroMentalState);
            info.Set("inspired", () => pawn.Inspired);
            info.Set("inspiration", () => pawn.Inspiration);
            info.Set("inspiration_def", () => pawn.InspirationDef);
            info.Set("job", () => GetPawnActivity(pawn));
            info.Set("job_raw", () => pawn.CurJob);
            info.Set("job_def", () => pawn.CurJobDef);
            info.Set("job_def_name", () => pawn.CurJobDef == null ? string.Empty : GetDefName(pawn.CurJobDef));
            info.Set("job_def_label", () => pawn.CurJobDef == null ? string.Empty : GetDefLabel(pawn.CurJobDef));

            info.Set("map", () => pawn.Map);
            info.Set("map_held", () => pawn.MapHeld);
            info.Set("position", () => pawn.Position);
            info.Set("position_held", () => pawn.PositionHeld);
            info.Set("health_scale", () => pawn.HealthScale);
            info.Set("age", () => CreateAgeInfo(pawn), false);
            info.Set("health", () => CreateHealthInfo(pawn), false);
            info.Set("needs", () => CreateNeedsInfo(pawn), false);
            info.Set("prompt", () => CreatePromptInfo(pawn), false);
            info.Set("location", () => RimTalkArtiPawnPromptData.GetLocation(pawn), false);
            info.Set("terrain", () => RimTalkArtiPawnPromptData.GetTerrain(pawn), false);
            info.Set("beauty", () => RimTalkArtiPawnPromptData.GetBeauty(pawn), false);
            info.Set("cleanliness", () => RimTalkArtiPawnPromptData.GetCleanliness(pawn), false);
            info.Set("surroundings", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn), false);
            info.Set("trackers", () => CreateTrackersInfo(pawn), false);
            info.Set("lifecycle", () => CreateLifecycleInfo(pawn), false);
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateAgeInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace age = new RimTalkArtiLazyNamespace();
            Pawn_AgeTracker tracker = pawn == null ? null : pawn.ageTracker;
            age.Set("exists", tracker != null);
            age.Set("raw", tracker);

            if (tracker == null)
            {
                RimTalkArtiLazyNamespace emptyBiological = CreateAgePeriodInfo(RimTalkArtiPawnAgeParts.FromTicks(0L));
                RimTalkArtiLazyNamespace emptyChronological = CreateAgePeriodInfo(RimTalkArtiPawnAgeParts.FromTicks(0L));
                age.Set("biological", emptyBiological);
                age.Set("bio", emptyBiological);
                age.Set("chronological", emptyChronological);
                age.Set("chrono", emptyChronological);
                return age;
            }

            RimTalkArtiPawnAgeParts biological = RimTalkArtiPawnAgeParts.FromTicks(tracker.AgeBiologicalTicks);
            RimTalkArtiPawnAgeParts chronological = RimTalkArtiPawnAgeParts.FromTicks(tracker.AgeChronologicalTicks);
            age.Set("biological", CreateAgePeriodInfo(biological));
            age.Set("bio", CreateAgePeriodInfo(biological));
            age.Set("chronological", CreateAgePeriodInfo(chronological));
            age.Set("chrono", CreateAgePeriodInfo(chronological));
            age.Set("biological_ticks", tracker.AgeBiologicalTicks);
            age.Set("biological_years", tracker.AgeBiologicalYears);
            age.Set("biological_years_float", tracker.AgeBiologicalYearsFloat);
            age.Set("biological_quadrums", biological.Quadrums);
            age.Set("biological_days", biological.Days);
            age.Set("biological_hours", biological.Hours);
            age.Set("chronological_ticks", tracker.AgeChronologicalTicks);
            age.Set("chronological_years", tracker.AgeChronologicalYears);
            age.Set("chronological_years_float", tracker.AgeChronologicalYearsFloat);
            age.Set("chronological_quadrums", chronological.Quadrums);
            age.Set("chronological_days", chronological.Days);
            age.Set("chronological_hours", chronological.Hours);
            age.Set("number", tracker.AgeNumberString ?? string.Empty);
            age.Set("number_string", tracker.AgeNumberString ?? string.Empty);
            age.Set("tooltip", tracker.AgeTooltipString ?? string.Empty);
            age.Set("birth_year", tracker.BirthYear);
            age.Set("birth_day_of_quadrum", tracker.BirthDayOfSeasonZeroBased + 1);
            age.Set("birth_day_of_quadrum_zero_based", tracker.BirthDayOfSeasonZeroBased);
            age.Set("birth_day_of_year", tracker.BirthDayOfYear + 1);
            age.Set("birth_day_of_year_zero_based", tracker.BirthDayOfYear);
            age.Set("birth_quadrum", tracker.BirthQuadrum.ToString());
            age.Set("life_stage", CreateLifeStageInfo(tracker));
            age.Set("life_stage_def", tracker.CurLifeStage);
            age.Set("life_stage_index", tracker.CurLifeStageIndex);
            age.Set("cur_life_stage_index", tracker.CurLifeStageIndex);
            age.Set("growth", tracker.Growth);
            age.Set("growth_tier", tracker.GrowthTier);
            age.Set("percent_to_next_growth_tier", tracker.PercentToNextGrowthTier);
            age.Set("at_max_growth_tier", tracker.AtMaxGrowthTier);
            age.Set("adult", tracker.Adult);
            age.Set("adult_min_age", tracker.AdultMinAge);
            age.Set("adult_min_age_ticks", tracker.AdultMinAgeTicks);
            age.Set("biological_ticks_per_tick", tracker.BiologicalTicksPerTick);
            age.Set("adult_aging_multiplier", tracker.AdultAgingMultiplier);
            age.Set("child_aging_multiplier", tracker.ChildAgingMultiplier);
            return age;
        }

        private static RimTalkArtiLazyNamespace CreateAgePeriodInfo(RimTalkArtiPawnAgeParts parts)
        {
            RimTalkArtiLazyNamespace period = new RimTalkArtiLazyNamespace();
            period.Set("ticks", parts.Ticks);
            period.Set("years", parts.Years);
            period.Set("quadrums", parts.Quadrums);
            period.Set("quadrum", parts.Quadrums);
            period.Set("days", parts.Days);
            period.Set("hours", parts.Hours);
            period.Set("total_years", parts.TotalYears);
            period.Set("total_quadrums", parts.TotalQuadrums);
            period.Set("total_days", parts.TotalDays);
            period.Set("total_hours", parts.TotalHours);
            return period;
        }

        private static RimTalkArtiLazyNamespace CreateLifeStageInfo(Pawn_AgeTracker tracker)
        {
            RimTalkArtiLazyNamespace stage = new RimTalkArtiLazyNamespace();
            LifeStageDef definition = tracker == null ? null : tracker.CurLifeStage;
            stage.Set("exists", definition != null);
            stage.Set("raw", definition);
            if (definition == null)
            {
                return stage;
            }

            stage.Set("def", definition);
            stage.Set("def_name", GetDefName(definition));
            stage.Set("label", GetDefLabel(definition));
            stage.Set("adjective", definition.Adjective ?? string.Empty);
            stage.Set("developmental_stage", definition.developmentalStage.ToString());
            stage.Set("reproductive", definition.reproductive);
            stage.Set("visible", definition.visible);
            stage.Set("always_downed", definition.alwaysDowned);
            stage.Set("claimable", definition.claimable);
            stage.Set("body_size_factor", definition.bodySizeFactor);
            stage.Set("health_scale_factor", definition.healthScaleFactor);
            stage.Set("hunger_rate_factor", definition.hungerRateFactor);
            return stage;
        }

        private static RimTalkArtiLazyNamespace CreateHealthInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace health = new RimTalkArtiLazyNamespace();
            Pawn_HealthTracker tracker = pawn == null ? null : pawn.health;
            health.Set("exists", tracker != null);
            health.Set("raw", tracker);
            if (tracker == null)
            {
                List<RimTalkArtiLazyNamespace> empty = new List<RimTalkArtiLazyNamespace>();
                health.Set("state", string.Empty);
                health.Set("hediffs", empty);
                health.Set("all_hediffs", empty);
                health.Set("visible_hediffs", empty);
                health.Set("hidden_hediffs", empty);
                health.Set("raw_hediffs", new List<Hediff>());
                health.Set("hediff_count", 0);
                health.Set("visible_hediff_count", 0);
                health.Set("hidden_hediff_count", 0);
                health.Set("has_hidden_hediffs", false);
                return health;
            }

            HediffSet hediffSet = tracker.hediffSet;
            health.Set("hediff_set", hediffSet);
            health.Set("capacities", tracker.capacities);
            health.Set("summary_health", tracker.summaryHealth);
            health.Set("surgery_bills", tracker.surgeryBills);
            health.Set("immunity", tracker.immunity);
            List<Hediff> all = hediffSet == null || hediffSet.hediffs == null
                ? new List<Hediff>()
                : new List<Hediff>(hediffSet.hediffs);
            List<Hediff> visible = new List<Hediff>();
            List<Hediff> hidden = new List<Hediff>();
            foreach (Hediff hediff in all)
            {
                if (hediff != null && hediff.Visible)
                {
                    visible.Add(hediff);
                }
                else if (hediff != null)
                {
                    hidden.Add(hediff);
                }
            }

            health.Set("state", tracker.State.ToString());
            health.Set("state_id", (int)tracker.State);
            health.Set("mobile", tracker.State == PawnHealthState.Mobile);
            health.Set("downed", tracker.Downed);
            health.Set("dead", tracker.Dead);
            health.Set("can_bleed", tracker.CanBleed);
            health.Set("in_pain_shock", tracker.InPainShock);
            health.Set("can_crawl", tracker.CanCrawl);
            health.Set("can_crawl_or_move", tracker.CanCrawlOrMove);
            health.Set("lethal_damage_threshold", tracker.LethalDamageThreshold);
            health.Set("health_scale", pawn.HealthScale);
            health.Set("pain_total", hediffSet == null ? 0f : hediffSet.PainTotal);
            health.Set("bleed_rate_total", hediffSet == null ? 0f : hediffSet.BleedRateTotal);
            health.Set("bleeding", hediffSet != null && hediffSet.BleedRateTotal > 0f);
            health.Set("hediff_count", all.Count);
            health.Set("visible_hediff_count", visible.Count);
            health.Set("hidden_hediff_count", hidden.Count);
            health.Set("has_hidden_hediffs", hidden.Count > 0);
            health.Set("hediffs", () => CreateHediffInfoList(all));
            health.Set("all_hediffs", () => CreateHediffInfoList(all));
            health.Set("visible_hediffs", () => CreateHediffInfoList(visible));
            health.Set("hidden_hediffs", () => CreateHediffInfoList(hidden));
            health.Set("raw_hediffs", all);
            health.Set("summary", () => ContextBuilder.GetHealthContext(pawn, PromptService.InfoLevel.Normal) ?? string.Empty, false);
            health.Set("detailed_summary", () => ContextBuilder.GetHealthContext(pawn, PromptService.InfoLevel.Full) ?? string.Empty, false);
            return health;
        }

        private static List<RimTalkArtiLazyNamespace> CreateHediffInfoList(IEnumerable<Hediff> hediffs)
        {
            List<RimTalkArtiLazyNamespace> result = new List<RimTalkArtiLazyNamespace>();
            if (hediffs == null)
            {
                return result;
            }

            foreach (Hediff hediff in hediffs)
            {
                result.Add(CreateHediffInfo(hediff));
            }

            return result;
        }

        private static RimTalkArtiLazyNamespace CreateHediffInfo(Hediff hediff)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", hediff != null);
            info.Set("raw", hediff);
            if (hediff == null)
            {
                return info;
            }

            HediffDef definition = hediff.def;
            info.Set("label", hediff.Label ?? string.Empty);
            info.Set("label_cap", hediff.LabelCap ?? string.Empty);
            info.Set("description", hediff.Description ?? string.Empty);
            info.Set("visible", hediff.Visible);
            info.Set("hidden", !hediff.Visible);
            info.Set("bleeding", hediff.Bleeding);
            info.Set("bleed_rate", hediff.BleedRate);
            info.Set("bleed_rate_scaled", hediff.BleedRateScaled);
            info.Set("pain_offset", hediff.PainOffset);
            info.Set("pain_factor", hediff.PainFactor);
            info.Set("severity", hediff.Severity);
            info.Set("severity_label", hediff.SeverityLabel ?? string.Empty);
            info.Set("stage_index", hediff.CurStageIndex);
            info.Set("stage", CreateHediffStageInfo(hediff.CurStage));
            info.Set("stage_raw", hediff.CurStage);
            info.Set("lethal", hediff.IsLethal);
            info.Set("currently_life_threatening", hediff.IsCurrentlyLifeThreatening);
            info.Set("summary_health_percent_impact", hediff.SummaryHealthPercentImpact);
            info.Set("tend_priority", hediff.TendPriority);
            info.Set("age_ticks", hediff.ageTicks);
            info.Set("age_days", (double)hediff.ageTicks / RimTalkArtiPawnAgeParts.TicksPerDay);
            info.Set("tick_added", hediff.tickAdded);
            info.Set("def", definition);
            info.Set("def_info", CreateHediffDefInfo(definition));
            info.Set("def_name", GetDefName(definition));
            info.Set("def_label", GetDefLabel(definition));
            info.Set("part", CreateBodyPartInfo(hediff.Part));
            info.Set("part_raw", hediff.Part);
            info.Set("part_label", hediff.Part == null ? string.Empty : hediff.Part.Label ?? string.Empty);
            info.Set("part_def_name", hediff.Part == null ? string.Empty : GetDefName(hediff.Part.def));
            info.Set("source", CreateHediffSourceInfo(hediff));
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateHediffDefInfo(HediffDef definition)
        {
            RimTalkArtiLazyNamespace info = CreateDefInfo(definition);
            if (definition == null)
            {
                return info;
            }

            info.Set("is_bad", definition.isBad);
            info.Set("chronic", definition.chronic);
            info.Set("tendable", definition.tendable);
            info.Set("initial_severity", definition.initialSeverity);
            info.Set("min_severity", definition.minSeverity);
            info.Set("max_severity", definition.maxSeverity);
            info.Set("lethal_severity", definition.lethalSeverity);
            info.Set("always_show_severity", definition.alwaysShowSeverity);
            info.Set("prevents_death", definition.preventsDeath);
            info.Set("prevents_crawling", definition.preventsCrawling);
            info.Set("prevents_pregnancy", definition.preventsPregnancy);
            info.Set("prevents_lung_rot", definition.preventsLungRot);
            info.Set("is_infection", definition.isInfection);
            info.Set("organic_added_bodypart", definition.organicAddedBodypart);
            info.Set("display_wound", definition.displayWound);
            info.Set("ever_curable_by_item", definition.everCurableByItem);
            info.Set("blocks_social_interaction", definition.blocksSocialInteraction);
            info.Set("blocks_sleeping", definition.blocksSleeping);
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateHediffStageInfo(HediffStage stage)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", stage != null);
            info.Set("raw", stage);
            if (stage == null)
            {
                return info;
            }

            info.Set("min_severity", stage.minSeverity);
            info.Set("label", stage.label ?? string.Empty);
            info.Set("override_label", stage.overrideLabel ?? string.Empty);
            info.Set("become_visible", stage.becomeVisible);
            info.Set("life_threatening", stage.lifeThreatening);
            info.Set("pain_factor", stage.painFactor);
            info.Set("pain_offset", stage.painOffset);
            info.Set("total_bleed_factor", stage.totalBleedFactor);
            info.Set("natural_healing_factor", stage.naturalHealingFactor);
            info.Set("regeneration", stage.regeneration);
            info.Set("blocks_mental_breaks", stage.blocksMentalBreaks);
            info.Set("blocks_inspirations", stage.blocksInspirations);
            info.Set("override_mood_base", stage.overrideMoodBase);
            info.Set("severity_gain_factor", stage.severityGainFactor);
            info.Set("prevent_vacuum_burns", stage.preventVacuumBurns);
            info.Set("blocks_sleeping", stage.blocksSleeping);
            info.Set("part_efficiency_offset", stage.partEfficiencyOffset);
            info.Set("part_ignore_missing_hp", stage.partIgnoreMissingHP);
            info.Set("destroy_part", stage.destroyPart);
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateBodyPartInfo(BodyPartRecord part)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", part != null);
            info.Set("raw", part);
            if (part == null)
            {
                return info;
            }

            info.Set("label", part.Label ?? string.Empty);
            info.Set("label_cap", part.LabelCap ?? string.Empty);
            info.Set("label_short", part.LabelShort ?? string.Empty);
            info.Set("index", part.Index);
            info.Set("def", part.def);
            info.Set("def_name", GetDefName(part.def));
            info.Set("def_label", GetDefLabel(part.def));
            info.Set("custom_label", part.customLabel ?? string.Empty);
            info.Set("height", part.height.ToString());
            info.Set("depth", part.depth.ToString());
            info.Set("coverage", part.coverage);
            info.Set("is_core_part", part.IsCorePart);
            info.Set("parent", CreateBodyPartInfo(part.parent));
            info.Set("parent_raw", part.parent);
            info.Set("parent_label", part.parent == null ? string.Empty : part.parent.Label ?? string.Empty);
            info.Set("parent_def_name", part.parent == null ? string.Empty : GetDefName(part.parent.def));
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateHediffSourceInfo(Hediff hediff)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("label", hediff == null ? string.Empty : hediff.sourceLabel ?? string.Empty);
            info.Set("def", hediff == null ? null : hediff.sourceDef);
            info.Set("def_name", hediff == null ? string.Empty : GetDefName(hediff.sourceDef));
            info.Set("body_part_group", hediff == null || hediff.sourceBodyPartGroup == null
                ? string.Empty
                : GetDefName(hediff.sourceBodyPartGroup));
            info.Set("tool_label", hediff == null ? string.Empty : hediff.sourceToolLabel ?? string.Empty);
            info.Set("hediff_def", hediff == null ? null : hediff.sourceHediffDef);
            info.Set("hediff_def_name", hediff == null ? string.Empty : GetDefName(hediff.sourceHediffDef));
            return info;
        }

        private static RimTalkArtiLazyNamespace CreateNeedsInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            Pawn_NeedsTracker tracker = pawn == null ? null : pawn.needs;
            info.Set("exists", tracker != null);
            info.Set("raw", tracker);
            if (tracker == null)
            {
                info.Set("all", new List<RimTalkArtiLazyNamespace>());
                info.Set("misc", new List<RimTalkArtiLazyNamespace>());
                info.Set("count", 0);
                info.Set("mood", CreateNeedInfo(null));
                return info;
            }

            List<Need> all = CopyNeeds(tracker.AllNeeds);
            List<Need> misc = CopyNeeds(tracker.MiscNeeds);
            info.Set("all", CreateNeedInfoList(all));
            info.Set("misc", CreateNeedInfoList(misc));
            info.Set("count", all.Count);
            info.Set("mood", CreateNeedInfo(tracker.mood));
            info.Set("mood_text", tracker.mood == null ? string.Empty : tracker.mood.MoodString ?? string.Empty);
            info.Set("mood_level", tracker.mood == null ? 0f : tracker.mood.CurLevel);
            info.Set("mood_level_percent", tracker.mood == null ? 0f : tracker.mood.CurLevelPercentage);
            info.Set("prefers_outdoors", tracker.PrefersOutdoors);
            info.Set("prefers_indoors", tracker.PrefersIndoors);
            return info;
        }

        private static List<Need> CopyNeeds(IEnumerable<Need> needs)
        {
            List<Need> result = new List<Need>();
            if (needs == null)
            {
                return result;
            }

            foreach (Need need in needs)
            {
                if (need != null)
                {
                    result.Add(need);
                }
            }

            return result;
        }

        private static List<RimTalkArtiLazyNamespace> CreateNeedInfoList(IEnumerable<Need> needs)
        {
            List<RimTalkArtiLazyNamespace> result = new List<RimTalkArtiLazyNamespace>();
            if (needs == null)
            {
                return result;
            }

            foreach (Need need in needs)
            {
                result.Add(CreateNeedInfo(need));
            }

            return result;
        }

        private static RimTalkArtiLazyNamespace CreateNeedInfo(Need need)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", need != null);
            info.Set("raw", need);
            if (need == null)
            {
                return info;
            }

            info.Set("label", need.LabelCap ?? string.Empty);
            info.Set("def", need.def);
            info.Set("def_name", GetDefName(need.def));
            info.Set("def_label", GetDefLabel(need.def));
            info.Set("description", need.def == null ? string.Empty : need.def.description ?? string.Empty);
            info.Set("level", need.CurLevel);
            info.Set("level_percent", need.CurLevelPercentage);
            info.Set("instant_level", need.CurInstantLevel);
            info.Set("instant_level_percent", need.CurInstantLevelPercentage);
            info.Set("max_level", need.MaxLevel);
            Need_Mood mood = need as Need_Mood;
            info.Set("mood", mood == null ? string.Empty : mood.MoodString ?? string.Empty);
            return info;
        }

        private static RimTalkArtiLazyNamespace CreatePromptInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace prompt = CreatePromptInfoAtLevel(pawn, PromptService.InfoLevel.Normal);
            prompt.Set("full", () => CreatePromptInfoAtLevel(pawn, PromptService.InfoLevel.Full), false);
            return prompt;
        }

        private static RimTalkArtiLazyNamespace CreatePromptInfoAtLevel(Pawn pawn, PromptService.InfoLevel level)
        {
            RimTalkArtiLazyNamespace prompt = new RimTalkArtiLazyNamespace();
            prompt.Set("context", () => PromptService.CreatePawnContext(pawn, level) ?? string.Empty, false);
            prompt.Set("race", () => ContextBuilder.GetRaceContext(pawn, level) ?? string.Empty, false);
            prompt.Set("genes", () => ContextBuilder.GetNotableGenesContext(pawn, level) ?? string.Empty, false);
            prompt.Set("all_genes", () => ContextBuilder.GetAllGenesContext(pawn, level) ?? string.Empty, false);
            prompt.Set("ideology", () => ContextBuilder.GetIdeologyContext(pawn, level) ?? string.Empty, false);
            prompt.Set("backstory", () => ContextBuilder.GetBackstoryContext(pawn, level) ?? string.Empty, false);
            prompt.Set("traits", () => ContextBuilder.GetTraitsContext(pawn, level) ?? string.Empty, false);
            prompt.Set("skills", () => ContextBuilder.GetSkillsContext(pawn, level) ?? string.Empty, false);
            prompt.Set("health", () => ContextBuilder.GetHealthContext(pawn, level) ?? string.Empty, false);
            prompt.Set("mood", () => ContextBuilder.GetMoodContext(pawn, level) ?? string.Empty, false);
            prompt.Set("thoughts", () => ContextBuilder.GetAllThoughtsContext(pawn) ?? string.Empty, false);
            prompt.Set("relations", () => ContextBuilder.GetRelationsContext(pawn, level) ?? string.Empty, false);
            prompt.Set("social", () => RelationsService.GetRelationsString(pawn) ?? string.Empty, false);
            prompt.Set("full_social", () => RelationsService.GetAllSocialString(pawn) ?? string.Empty, false);
            prompt.Set("full_relation", () => RelationsService.GetAllRelationsString(pawn) ?? string.Empty, false);
            prompt.Set("full_interaction", () => RelationsService.GetAllInteractionString(pawn) ?? string.Empty, false);
            prompt.Set("equipment", () => ContextBuilder.GetEquipmentContext(pawn, level) ?? string.Empty, false);
            prompt.Set("captive_status", () => ContextBuilder.GetPrisonerSlaveContext(pawn, level) ?? string.Empty, false);
            prompt.Set("activity", () => GetPawnActivity(pawn), false);
            prompt.Set("location", () => RimTalkArtiPawnPromptData.GetLocation(pawn), false);
            prompt.Set("terrain", () => RimTalkArtiPawnPromptData.GetTerrain(pawn), false);
            prompt.Set("beauty", () => RimTalkArtiPawnPromptData.GetBeauty(pawn), false);
            prompt.Set("cleanliness", () => RimTalkArtiPawnPromptData.GetCleanliness(pawn), false);
            prompt.Set("surroundings", () => RimTalkArtiPawnPromptData.GetNearbyThingsText(pawn), false);
            return prompt;
        }

        private static RimTalkArtiLazyNamespace CreateTrackersInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace trackers = new RimTalkArtiLazyNamespace();
            trackers.Set("age", () => pawn.ageTracker, false);
            trackers.Set("health", () => pawn.health, false);
            trackers.Set("records", () => pawn.records, false);
            trackers.Set("inventory", () => pawn.inventory, false);
            trackers.Set("melee_verbs", () => pawn.meleeVerbs, false);
            trackers.Set("verb_tracker", () => pawn.verbTracker, false);
            trackers.Set("ownership", () => pawn.ownership, false);
            trackers.Set("carry", () => pawn.carryTracker, false);
            trackers.Set("needs", () => pawn.needs, false);
            trackers.Set("mind_state", () => pawn.mindState, false);
            trackers.Set("surroundings", () => pawn.surroundings, false);
            trackers.Set("thinker", () => pawn.thinker, false);
            trackers.Set("jobs", () => pawn.jobs, false);
            trackers.Set("stances", () => pawn.stances, false);
            trackers.Set("infection_vectors", () => pawn.infectionVectors, false);
            trackers.Set("duplicate", () => pawn.duplicate, false);
            trackers.Set("rotation", () => pawn.rotationTracker, false);
            trackers.Set("pather", () => pawn.pather, false);
            trackers.Set("natives", () => pawn.natives, false);
            trackers.Set("filth", () => pawn.filth, false);
            trackers.Set("roping", () => pawn.roping, false);
            trackers.Set("flight", () => pawn.flight, false);
            trackers.Set("equipment", () => pawn.equipment, false);
            trackers.Set("apparel", () => pawn.apparel, false);
            trackers.Set("skills", () => pawn.skills, false);
            trackers.Set("story", () => pawn.story, false);
            trackers.Set("guest", () => pawn.guest, false);
            trackers.Set("guilt", () => pawn.guilt, false);
            trackers.Set("royalty", () => pawn.royalty, false);
            trackers.Set("abilities", () => pawn.abilities, false);
            trackers.Set("ideo", () => pawn.ideo, false);
            trackers.Set("genes", () => pawn.genes, false);
            trackers.Set("creep_joiner", () => pawn.creepjoiner, false);
            trackers.Set("work_settings", () => pawn.workSettings, false);
            trackers.Set("trader", () => pawn.trader, false);
            trackers.Set("style", () => pawn.style, false);
            trackers.Set("style_observer", () => pawn.styleObserver, false);
            trackers.Set("connections", () => pawn.connections, false);
            trackers.Set("training", () => pawn.training, false);
            trackers.Set("caller", () => pawn.caller, false);
            trackers.Set("psychic_entropy", () => pawn.psychicEntropy, false);
            trackers.Set("mutant", () => pawn.mutant, false);
            trackers.Set("relations", () => pawn.relations, false);
            trackers.Set("interactions", () => pawn.interactions, false);
            trackers.Set("player_settings", () => pawn.playerSettings, false);
            trackers.Set("outfits", () => pawn.outfits, false);
            trackers.Set("drugs", () => pawn.drugs, false);
            trackers.Set("food_restriction", () => pawn.foodRestriction, false);
            trackers.Set("timetable", () => pawn.timetable, false);
            trackers.Set("inventory_stock", () => pawn.inventoryStock, false);
            trackers.Set("mechanitor", () => pawn.mechanitor, false);
            trackers.Set("learning", () => pawn.learning, false);
            trackers.Set("reading", () => pawn.reading, false);
            trackers.Set("drafter", () => pawn.drafter, false);
            trackers.Set("lord", () => pawn.lord, false);
            return trackers;
        }

        private static RimTalkArtiLazyNamespace CreateLifecycleInfo(Pawn pawn)
        {
            RimTalkArtiLazyNamespace lifecycle = new RimTalkArtiLazyNamespace();
            lifecycle.Set("spawned", pawn.Spawned);
            lifecycle.Set("spawned_or_any_parent_spawned", pawn.SpawnedOrAnyParentSpawned);
            lifecycle.Set("destroyed", pawn.Destroyed);
            lifecycle.Set("suspended", pawn.Suspended);
            lifecycle.Set("marked_for_discard", pawn.markedForDiscard);
            lifecycle.Set("teleporting", pawn.teleporting);
            lifecycle.Set("became_world_pawn_tick_abs", pawn.becameWorldPawnTickAbs);
            lifecycle.Set("previous_map", pawn.prevMap);
            return lifecycle;
        }

        private static RimTalkArtiLazyNamespace CreateDefInfo(Def definition)
        {
            RimTalkArtiLazyNamespace info = new RimTalkArtiLazyNamespace();
            info.Set("exists", definition != null);
            info.Set("raw", definition);
            if (definition != null)
            {
                info.Set("def_name", GetDefName(definition));
                info.Set("label", GetDefLabel(definition));
                info.Set("description", definition.description ?? string.Empty);
            }

            return info;
        }

        private static string GetDefName(Def definition)
        {
            return definition == null ? string.Empty : definition.defName ?? string.Empty;
        }

        private static string GetDefLabel(Def definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(definition.label))
            {
                return definition.defName ?? string.Empty;
            }

            return definition.LabelCap.RawText ?? definition.label;
        }

        private static string GetFactionName(Faction faction)
        {
            return faction == null ? string.Empty : faction.Name ?? string.Empty;
        }

        private static string GetRaceLabel(Pawn pawn)
        {
            if (pawn == null)
            {
                return string.Empty;
            }

            if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.Xenotype != null)
            {
                return pawn.genes.XenotypeLabel ?? string.Empty;
            }

            return GetDefLabel(pawn.def);
        }

        private static string GetMentalStateText(Pawn pawn)
        {
            if (pawn == null || !pawn.InMentalState || pawn.MentalState == null)
            {
                return string.Empty;
            }

            return pawn.MentalState.InspectLine ?? string.Empty;
        }

        private static string GetPawnActivity(Pawn pawn)
        {
            if (pawn == null)
            {
                return string.Empty;
            }

            if (pawn.InMentalState)
            {
                return pawn.MentalState == null ? string.Empty : pawn.MentalState.InspectLine ?? string.Empty;
            }

            if (pawn.CurJobDef == null || pawn.jobs == null || pawn.jobs.curDriver == null)
            {
                return string.Empty;
            }

            return Describer.StripConditionSuffix(pawn.jobs.curDriver.GetReport()) ?? string.Empty;
        }
    }
}
