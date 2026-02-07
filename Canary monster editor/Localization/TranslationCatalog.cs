using System.Collections.Generic;

namespace Canary_monster_editor.Localization
{
    public enum TranslationCulture_t
    {
        Portuguese = 0,
        English = 1,
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
    }

    public static class TranslationCatalog
    {
        private static readonly IReadOnlyDictionary<TranslationCulture_t, IReadOnlyDictionary<TranslationDictionaryIndex, string>> Catalog
            = new Dictionary<TranslationCulture_t, IReadOnlyDictionary<TranslationDictionaryIndex, string>>
        {
            [TranslationCulture_t.Portuguese] = new Dictionary<TranslationDictionaryIndex, string>
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
            },
            [TranslationCulture_t.English] = new Dictionary<TranslationDictionaryIndex, string>
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
            },
        };

        private static TranslationCulture_t _currentCulture = TranslationCulture_t.Portuguese;

        public static TranslationCulture_t CurrentCulture
        {
            get => _currentCulture;
            set => _currentCulture = value;
        }

        public static string GetText(TranslationDictionaryIndex index)
        {
            return GetText(_currentCulture, index);
        }

        public static string GetText(TranslationCulture_t culture, TranslationDictionaryIndex index)
        {
            if (!Catalog.TryGetValue(culture, out var dictionary))
            {
                return "--";
            }

            if (!dictionary.TryGetValue(index, out var translation))
            {
                return "--";
            }

            return translation;
        }

        public static bool TryGetText(TranslationDictionaryIndex index, out string translation)
        {
            var text = GetText(index);
            var hasValue = text != "--";
            translation = hasValue ? text : string.Empty;
            return hasValue;
        }
    }
}

