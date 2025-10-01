using System;
using System.Collections.Generic;
using System.Linq;
using Tibia.Protobuf.Staticdata;

namespace Canary_monster_editor.Services
{
    internal static class StaticDataRepository
    {
        public static bool HasMonsters(StaticData data)
        {
            return data?.Monster?.Count > 0;
        }

        public static bool HasBosses(StaticData data)
        {
            return data?.Boss?.Count > 0;
        }

        public static Monster FindMonster(StaticData data, Func<Monster, bool> predicate)
        {
            if (data?.Monster == null || predicate == null)
            {
                return null;
            }

            foreach (var monster in data.Monster)
            {
                if (predicate(monster))
                {
                    return monster;
                }
            }

            return null;
        }

        public static Boss FindBoss(StaticData data, Func<Boss, bool> predicate)
        {
            if (data?.Boss == null || predicate == null)
            {
                return null;
            }

            foreach (var boss in data.Boss)
            {
                if (predicate(boss))
                {
                    return boss;
                }
            }

            return null;
        }

        public static bool RemoveMonster(StaticData data, Func<Monster, bool> predicate)
        {
            if (data?.Monster == null || predicate == null)
            {
                return false;
            }

            var monster = FindMonster(data, predicate);
            if (monster == null)
            {
                return false;
            }

            return data.Monster.Remove(monster);
        }

        public static bool RemoveBoss(StaticData data, Func<Boss, bool> predicate)
        {
            if (data?.Boss == null || predicate == null)
            {
                return false;
            }

            var boss = FindBoss(data, predicate);
            if (boss == null)
            {
                return false;
            }

            return data.Boss.Remove(boss);
        }

        public static uint GetHighestCreatureId(StaticData data)
        {
            if (data == null)
            {
                return 0;
            }

            var maxMonsterId = data.Monster != null && data.Monster.Count > 0
                ? data.Monster.Max(monster => monster.Raceid)
                : 0;
            var maxBossId = data.Boss != null && data.Boss.Count > 0
                ? data.Boss.Max(boss => boss.Id)
                : 0;
            return Math.Max(maxMonsterId, maxBossId);
        }

        public static bool HasBossAppearanceObjects(StaticData data)
        {
            if (data?.Boss == null)
            {
                return false;
            }

            foreach (var boss in data.Boss)
            {
                if (boss.AppearanceType != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static Monster CreateDefaultMonster(uint id, string name)
        {
            return new Monster
            {
                Raceid = id,
                Name = name,
                AppearanceType = CreateDefaultAppearanceType(),
            };
        }

        public static Boss CreateDefaultBoss(uint id, string name, bool includeAppearance)
        {
            return new Boss
            {
                Id = id,
                Name = name,
                AppearanceType = includeAppearance ? CreateDefaultAppearanceType() : null,
            };
        }

        private static Appearance_Type CreateDefaultAppearanceType()
        {
            return new Appearance_Type
            {
                Outfittype = 1,
                Itemtype = 0,
                Outfitaddon = 0,
                Colors = new Colors
                {
                    Lookhead = 0,
                    Lookbody = 0,
                    Looklegs = 0,
                    Lookfeet = 0,
                },
            };
        }
    }
}

