using System;
using System.Collections.Generic;
using Tibia.Protobuf.Staticdata;
using Google.Protobuf;
using System.IO;

namespace Canary_monster_editor
{
    public class Data
    {
        public static StaticData GlobalStaticData { get; set; }
        public static uint GlobalLastCreatureId { get; set; }
        public static bool GlobalBossAppearancesObjects { get; set; } = false;
        public static DateTime GlobalFileLastTimeEdited { get; set; } = DateTime.Now;
        public static string GlobalStaticDataPath { get; set; } = "----";
        public static string GlobalVersion { get { return "v1.0"; } }
        public static TranslationCulture_t GlobalTranslationType { get; set; } = TranslationCulture_t.Portuguese;

        #region Protobuf load/save
        public static bool LoadStaticDataProbufBinaryFileFromPath(string path)
        {
            if (GlobalStaticData != null)
            {
                return false;
            }

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                GlobalStaticData = StaticData.Parser.ParseFrom(fileStream);

            if (GlobalStaticData == null || GlobalStaticData.Monster == null || GlobalStaticData.Monster.Count == 0)
            {
                return false;
            }

            // Reinicialize para garantir o valor máximo
            GlobalLastCreatureId = 0;

            foreach (var monster in GlobalStaticData.Monster)
            {
                if (monster.Raceid > GlobalLastCreatureId)
                {
                    GlobalLastCreatureId = monster.Raceid;
                }
            }

            foreach (var boss in GlobalStaticData.Boss)
            {
                if (boss.Id > GlobalLastCreatureId)
                {
                    GlobalLastCreatureId = boss.Id;
                }

                if (boss.AppearanceType != null)
                {
                    GlobalBossAppearancesObjects = true;
                }
            }

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

            using (FileStream fileStream = new FileStream(GlobalStaticDataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                GlobalStaticData.WriteTo(fileStream);

            GlobalFileLastTimeEdited = File.GetLastWriteTime(GlobalStaticDataPath);
            return true;
        }
        #endregion

        #region Get objects
        public static Monster GetMonsterByRaceId(uint id)
        {
            if (id == 0 || GlobalStaticData == null || GlobalStaticData.Monster == null || GlobalStaticData.Monster.Count == 0) {
                return null;
            }

            foreach (var monster in GlobalStaticData.Monster) {
                if (monster.Raceid == id) {
                    return monster;
                }
            }

            return null;
        }
        public static Monster GetMonsterByName(string name)
        {
            if (string.IsNullOrEmpty(name) || GlobalStaticData == null || GlobalStaticData.Monster == null || GlobalStaticData.Monster.Count == 0) {
                return null;
            }

            foreach (var monster in GlobalStaticData.Monster) {
                if (monster.Name.ToLower() == name.ToLower()) {
                    return monster;
                }
            }

            return null;
        }
        public static Boss GetBossById(uint id)
        {
            if (id == 0 || GlobalStaticData == null || GlobalStaticData.Boss == null || GlobalStaticData.Boss.Count == 0) {
                return null;
            }

            foreach (var boss in GlobalStaticData.Boss) {
                if (boss.Id == id) {
                    return boss;
                }
            }

            return null;
        }
        public static Boss GetBossByName(string name)
        {
            if (string.IsNullOrEmpty(name) || GlobalStaticData == null || GlobalStaticData.Boss == null || GlobalStaticData.Boss.Count == 0) {
                return null;
            }

            foreach (var boss in GlobalStaticData.Boss) {
                if (boss.Name.ToLower() == name.ToLower()) {
                    return boss;
                }
            }

            return null;
        }
        #endregion

        #region Delete objects
        public static bool DeleteMonsterByRaceId(uint id)
        {
            if (id == 0 || GlobalStaticData == null || GlobalStaticData.Monster == null || GlobalStaticData.Monster.Count == 0) {
                return false;
            }

            foreach (var monster in GlobalStaticData.Monster) {
                if (monster.Raceid == id) {
                    return GlobalStaticData.Monster.Remove(monster);
                }
            }

            return false;
        }
        public static bool DeleteMonsterByName(string name)
        {
            if (string.IsNullOrEmpty(name) || GlobalStaticData == null || GlobalStaticData.Monster == null || GlobalStaticData.Monster.Count == 0) {
                return false;
            }

            foreach (var monster in GlobalStaticData.Monster) {
                if (monster.Name.ToLower() == name.ToLower()) {
                    return GlobalStaticData.Monster.Remove(monster);
                }
            }

            return false;
        }
        public static bool DeleteBossByd(uint id)
        {
            if (id == 0 || GlobalStaticData == null || GlobalStaticData.Boss == null || GlobalStaticData.Boss.Count == 0) {
                return false;
            }

            foreach (var boss in GlobalStaticData.Boss) {
                if (boss.Id == id) {
                    return GlobalStaticData.Boss.Remove(boss);
                }
            }

            return false;
        }
        public static bool DeleteBossByName(string name)
        {
            if (string.IsNullOrEmpty(name) || GlobalStaticData == null || GlobalStaticData.Boss == null || GlobalStaticData.Boss.Count == 0) {
                return false;
            }

            foreach (var boss in GlobalStaticData.Boss) {
                if (boss.Name.ToLower() == name.ToLower()) {
                    return GlobalStaticData.Boss.Remove(boss);
                }
            }

            return false;
        }
        #endregion

        #region Create objects
        public static void CreateBrandNewMonster()
        {
            GlobalLastCreatureId++;
            GlobalStaticData.Monster.Add(new Monster()
            {
                Raceid = GlobalLastCreatureId,
                Name = "Brand-new monster #" + GlobalLastCreatureId,
                AppearanceType = new Appearance_Type()
                {
                    Outfittype = 1,
                    Itemtype = 0,
                    Outfitaddon = 0,
                    Colors = new Colors()
                    {
                        Lookhead = 0,
                        Lookbody = 0,
                        Looklegs = 0,
                        Lookfeet = 0
                    }
                }
            });
        }
        public static void CreateBrandNewBoss()
        {
            GlobalLastCreatureId++;
            GlobalStaticData.Boss.Add(new Boss()
            {
                Id = GlobalLastCreatureId,
                Name = "Brand-new boss #" + GlobalLastCreatureId,
                AppearanceType = !GlobalBossAppearancesObjects ? null : (new Appearance_Type()
                {
                    Outfittype = 1,
                    Itemtype = 0,
                    Outfitaddon = 0,
                    Colors = new Colors()
                    {
                        Lookhead = 0,
                        Lookbody = 0,
                        Looklegs = 0,
                        Lookfeet = 0
                    }
                })
            });
        }
        #endregion

        #region Culture (Translation)
        public enum TranslationCulture_t
        {
            Portuguese = 0,
            English = 1
        }
        public enum TranslationDictionaryIndex
        {
            Open = 0,
            Save = 1,
            Delete = 2,
            New = 3,
            Monsters = 4,
            Monster = 5,
            Bosses = 6,
            Boss = 7,
            Name = 8,
            FileOpenned = 9,
            LastSaved = 10,
            DiscardUnsavedChanges = 11,
            DiscardUnsavedChangesTitle = 12,
            DeleteObject = 13,
            DeleteObjectTitle = 14,
            NewObject = 15,
            NewObjectTitle = 16,
            DiscardUnsavedChangesOnAssets = 17,
            DiscardUnsavedChangesOnAssetsTitle = 18,
            Author = 19,
            SelectStaticDataFile = 20,
            SelectStaticDataFileFilter = 21,
            BossAppearanceDisabled = 22,
            Compile = 23,
            ExportImport = 24,
            SelectAll = 25,
            DeselectAll = 26,
            ExportSelected = 27,
            SearchPlaceholder = 28,
        }
        public static readonly Dictionary<TranslationDictionaryIndex, string> TranslationDictionary_portuguese = new Dictionary<TranslationDictionaryIndex, string>
        {
            [TranslationDictionaryIndex.Open] = "Abrir",
            [TranslationDictionaryIndex.Save] = "Salvar",
            [TranslationDictionaryIndex.Delete] = "Deletar",
            [TranslationDictionaryIndex.New] = "Novo",
            [TranslationDictionaryIndex.Monsters] = "Monstros",
            [TranslationDictionaryIndex.Monster] = "Monstro",
            [TranslationDictionaryIndex.Bosses] = "Chefes",
            [TranslationDictionaryIndex.Boss] = "Chefe",
            [TranslationDictionaryIndex.Name] = "Nome: ",
            [TranslationDictionaryIndex.FileOpenned] = "Arquivo aberto: ",
            [TranslationDictionaryIndex.LastSaved] = "Salvo em: ",
            [TranslationDictionaryIndex.DiscardUnsavedChanges] = "Você tem mudanças não salvas no {0} selecionado, tem certeza que quer descartar estas mudanças?\n Esta ação é irreversivel!",
            [TranslationDictionaryIndex.DiscardUnsavedChangesTitle] = "Descartar mudanças não salvas",
            [TranslationDictionaryIndex.DeleteObject] = "Você tem certeza que deseja deletar o {0} com ID: {1} de nome {2} ?\n Esta ação é irreversivel!",
            [TranslationDictionaryIndex.DeleteObjectTitle] = "Deletar criatura",
            [TranslationDictionaryIndex.NewObject] = "Você tem certeza que deseja criar um {0} novo ?",
            [TranslationDictionaryIndex.NewObjectTitle] = "Criar criatura nova",
            [TranslationDictionaryIndex.DiscardUnsavedChangesOnAssets] = "Você tem dados não salvos no seu Assets, tem certeza que deseja fechar a aplicação e descartar sua alterações?",
            [TranslationDictionaryIndex.DiscardUnsavedChangesOnAssetsTitle] = "Discartar e fechar aplicação",
            [TranslationDictionaryIndex.Author] = "Autor: ",
            [TranslationDictionaryIndex.SelectStaticDataFile] = "Selecione o arquivo 'staticdata-XXXXX.dat' do assets do seu client",
            [TranslationDictionaryIndex.SelectStaticDataFileFilter] = "Arquivo DAT (*.dat)|*.dat",
            [TranslationDictionaryIndex.BossAppearanceDisabled] = "Os dados de aparencia dos chefes não estão disponivels pois sua versão de client é inferior a 12.90.",
            [TranslationDictionaryIndex.Compile] = "Compilar",
            [TranslationDictionaryIndex.ExportImport] = "Exportar/Importar",
            [TranslationDictionaryIndex.SelectAll] = "Selecionar Todos",
            [TranslationDictionaryIndex.DeselectAll] = "Desmarcar Todos",
            [TranslationDictionaryIndex.ExportSelected] = "Exportar Selecionados",
            [TranslationDictionaryIndex.SearchPlaceholder] = "Buscar por nome ou ID..."
        };
        public static readonly Dictionary<TranslationDictionaryIndex, string> TranslationDictionary_english = new Dictionary<TranslationDictionaryIndex, string>
        {
            [TranslationDictionaryIndex.Open] = "Open",
            [TranslationDictionaryIndex.Save] = "Save",
            [TranslationDictionaryIndex.Delete] = "Delete",
            [TranslationDictionaryIndex.New] = "New",
            [TranslationDictionaryIndex.Monsters] = "Monsters",
            [TranslationDictionaryIndex.Monster] = "Monster",
            [TranslationDictionaryIndex.Bosses] = "Bosses",
            [TranslationDictionaryIndex.Boss] = "Boss",
            [TranslationDictionaryIndex.Name] = "Name: ",
            [TranslationDictionaryIndex.FileOpenned] = "File open: ",
            [TranslationDictionaryIndex.LastSaved] = "Last save: ",
            [TranslationDictionaryIndex.DiscardUnsavedChanges] = "You have unsaved data on your selected {0}, are you sure you wan't to discard your changes?\n This action is irreversible!",
            [TranslationDictionaryIndex.DiscardUnsavedChangesTitle] = "Discard unsaved changes",
            [TranslationDictionaryIndex.DeleteObject] = "Are you sure you want to delete the {0} with ID: {1} named as {2} ?\n This action is irreversible!",
            [TranslationDictionaryIndex.DeleteObjectTitle] = "Delete creature",
            [TranslationDictionaryIndex.NewObject] = "Are you sure you want to create a brand-new {0} ?",
            [TranslationDictionaryIndex.NewObjectTitle] = "New creature",
            [TranslationDictionaryIndex.DiscardUnsavedChangesOnAssets] = "You have unsaved data on your assets, are you sure you wan't to close the application and discard your changes?",
            [TranslationDictionaryIndex.DiscardUnsavedChangesOnAssetsTitle] = "Discard and close",
            [TranslationDictionaryIndex.Author] = "Author: ",
            [TranslationDictionaryIndex.SelectStaticDataFile] = "Select your client assets 'staticdata-XXXXX.dat' file",
            [TranslationDictionaryIndex.SelectStaticDataFileFilter] = "DAT file (*.dat)|*.dat",
            [TranslationDictionaryIndex.BossAppearanceDisabled] = "The bosses appearances data are disabled due to your client version being lower then 12.90.",
            [TranslationDictionaryIndex.Compile] = "Compile",
            [TranslationDictionaryIndex.ExportImport] = "Export/Import",
            [TranslationDictionaryIndex.SelectAll] = "Select All",
            [TranslationDictionaryIndex.DeselectAll] = "Deselect All",
            [TranslationDictionaryIndex.ExportSelected] = "Export Selected",
            [TranslationDictionaryIndex.SearchPlaceholder] = "Search by name or ID..."
        };
        public static string GetCultureText(TranslationDictionaryIndex index)
        {
            string rt = "--";
            switch (GlobalTranslationType) {
                case TranslationCulture_t.Portuguese: {
                        rt = TranslationDictionary_portuguese[index];
                        break;
                    };
                case TranslationCulture_t.English: {
                        rt = TranslationDictionary_english[index];
                        break;
                    };
                default:
                    break;
            }

            return rt;
        }
        #endregion
    }
}
