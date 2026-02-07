using System;
using System.IO;
using Canary_monster_editor.Localization;
using Canary_monster_editor.Services;
using Google.Protobuf;
using Tibia.Protobuf.Staticdata;

namespace Canary_monster_editor
{
    public class Data
    {
        public static StaticData GlobalStaticData { get; set; }
        public static uint GlobalLastCreatureId { get; set; }
        public static bool GlobalBossAppearancesObjects { get; set; } = false;
        public static DateTime GlobalFileLastTimeEdited { get; set; } = DateTime.Now;
        public static string GlobalStaticDataPath { get; set; } = "----";
        public static string GlobalVersion
        {
            get { return "v1.0"; }
        }

        public static TranslationCulture_t GlobalTranslationType
        {
            get => TranslationCatalog.CurrentCulture;
            set => TranslationCatalog.CurrentCulture = value;
        }

        #region Protobuf load/save
        public static bool LoadStaticDataProbufBinaryFileFromPath(string path)
        {
            if (GlobalStaticData != null)
            {
                return false;
            }

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                GlobalStaticData = StaticData.Parser.ParseFrom(fileStream);
            }

            if (!StaticDataRepository.HasMonsters(GlobalStaticData))
            {
                return false;
            }

            GlobalLastCreatureId = StaticDataRepository.GetHighestCreatureId(GlobalStaticData);
            GlobalBossAppearancesObjects = StaticDataRepository.HasBossAppearanceObjects(GlobalStaticData);
            GlobalStaticDataPath = path;
            GlobalFileLastTimeEdited = File.GetLastWriteTime(path);
            return true;
        }

        public static bool SaveStaticDataProtobufBinaryFile()
        {
            if (string.IsNullOrEmpty(GlobalStaticDataPath) || GlobalStaticData == null)
            {
                return false;
            }

            using (FileStream fileStream = new FileStream(GlobalStaticDataPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                GlobalStaticData.WriteTo(fileStream);
            }

            GlobalFileLastTimeEdited = File.GetLastWriteTime(GlobalStaticDataPath);
            return true;
        }
        #endregion

        #region Get objects
        public static Monster GetMonsterByRaceId(uint id)
        {
            if (id == 0)
            {
                return null;
            }

            return StaticDataRepository.FindMonster(GlobalStaticData, monster => monster.Raceid == id);
        }

        public static Monster GetMonsterByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return StaticDataRepository.FindMonster(GlobalStaticData, monster => string.Equals(monster.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static Boss GetBossById(uint id)
        {
            if (id == 0)
            {
                return null;
            }

            return StaticDataRepository.FindBoss(GlobalStaticData, boss => boss.Id == id);
        }

        public static Boss GetBossByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return StaticDataRepository.FindBoss(GlobalStaticData, boss => string.Equals(boss.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Delete objects
        public static bool DeleteMonsterByRaceId(uint id)
        {
            if (id == 0)
            {
                return false;
            }

            return StaticDataRepository.RemoveMonster(GlobalStaticData, monster => monster.Raceid == id);
        }

        public static bool DeleteMonsterByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return StaticDataRepository.RemoveMonster(GlobalStaticData, monster => string.Equals(monster.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool DeleteBossByd(uint id)
        {
            if (id == 0)
            {
                return false;
            }

            return StaticDataRepository.RemoveBoss(GlobalStaticData, boss => boss.Id == id);
        }

        public static bool DeleteBossByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return StaticDataRepository.RemoveBoss(GlobalStaticData, boss => string.Equals(boss.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Create objects
        public static void CreateBrandNewMonster()
        {
            GlobalLastCreatureId++;
            EnsureStaticDataCollections();
            GlobalStaticData.Monster.Add(StaticDataRepository.CreateDefaultMonster(GlobalLastCreatureId, "Brand-new monster #" + GlobalLastCreatureId));
        }

        public static void CreateBrandNewBoss()
        {
            GlobalLastCreatureId++;
            EnsureStaticDataCollections();
            GlobalStaticData.Boss.Add(StaticDataRepository.CreateDefaultBoss(GlobalLastCreatureId, "Brand-new boss #" + GlobalLastCreatureId, GlobalBossAppearancesObjects));
        }

        private static void EnsureStaticDataCollections()
        {
            if (GlobalStaticData == null)
            {
                GlobalStaticData = new StaticData();
            }

        }
        #endregion

        #region Culture (Translation)
        public static string GetCultureText(TranslationDictionaryIndex index)
        {
            return TranslationCatalog.GetText(index);
        }
        #endregion
    }
}

