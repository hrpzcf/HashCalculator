using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace HashCalculator
{
    internal class ShellMenuEditorModel : NotifiableModel
    {
        private HcCtxMenuModel _selectedMenu;
        private ObservableCollection<HcCtxMenuModel> _menuList;
        private static readonly Encoding menuEncoding = Encoding.Unicode;
        private RelayCommand _resetMenusCmd;
        private RelayCommand _saveMenuListCmd;
        private RelayCommand _addMenuCmd;
        private RelayCommand _deleteMenuCmd;
        private RelayCommand _moveMenuUpCmd;
        private RelayCommand _moveMenuDownCmd;
        private RelayCommand _editMenuPropCmd;

        public ShellMenuEditorModel(Window parent)
        {
            this.Parent = parent;
            if (this.LoadMenuListFromJsonFile() is string reason)
            {
                MessageBox.Show(parent, $"载入快捷菜单配置文件失败：{reason}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public Window Parent { get; }

        public ObservableCollection<HcCtxMenuModel> MenuList
        {
            get
            {
                return this._menuList;
            }
            private set
            {
                this.SetPropNotify(ref this._menuList, value);
            }
        }

        public HcCtxMenuModel SelectedMenu
        {
            get
            {
                return this._selectedMenu;
            }
            set
            {
                this.SetPropNotify(ref this._selectedMenu, value);
            }
        }

        private void SaveMenusAction(object param)
        {
            string reasonForFailure = this.SaveMenuListToJsonFile();
            if (string.IsNullOrEmpty(reasonForFailure))
            {
                MessageBox.Show(this.Parent, "快捷菜单配置文件已保存！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(this.Parent, $"配置文件未保存：\n{reasonForFailure}", "警告",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public ICommand SaveMenuListCmd
        {
            get
            {
                if (this._saveMenuListCmd == null)
                {
                    this._saveMenuListCmd = new RelayCommand(this.SaveMenusAction);
                }
                return this._saveMenuListCmd;
            }
        }

        private void ResetMenusAction(object param)
        {
            this.ManuallyResetMenuList();
            MessageBox.Show(this.Parent, "快捷菜单编辑列表已重置！", "提示", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public ICommand ResetMenuListCmd
        {
            get
            {
                if (this._resetMenusCmd == null)
                {
                    this._resetMenusCmd = new RelayCommand(this.ResetMenusAction);
                }
                return this._resetMenusCmd;
            }
        }

        private void AddMenuAction(object param)
        {
            HcCtxMenuModel hcCtxMenuModel = new HcCtxMenuModel();
            if (this.MenuList == null)
            {
                this.MenuList = new ObservableCollection<HcCtxMenuModel>();
            }
            this.MenuList.Add(hcCtxMenuModel);
            this.SelectedMenu = hcCtxMenuModel;
        }

        public ICommand AddMenuListCmd
        {
            get
            {
                if (this._addMenuCmd == null)
                {
                    this._addMenuCmd = new RelayCommand(this.AddMenuAction);
                }
                return this._addMenuCmd;
            }
        }

        private void DeleteMenuAction(object param)
        {
            if (this.MenuList != null)
            {
                int index;
                if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1)
                {
                    this.MenuList.RemoveAt(index);
                    if (index < this.MenuList.Count)
                    {
                        this.SelectedMenu = this.MenuList[index];
                    }
                    else if (index > 0)
                    {
                        this.SelectedMenu = this.MenuList[index - 1];
                    }
                }
                else
                {
                    MessageBox.Show(this.Parent, "没有选择任何主菜单！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        public ICommand DeleteMenuCmd
        {
            get
            {
                if (this._deleteMenuCmd == null)
                {
                    this._deleteMenuCmd = new RelayCommand(this.DeleteMenuAction);
                }
                return this._deleteMenuCmd;
            }
        }

        private void MoveMenuUpAction(object param)
        {
            if (this.MenuList != null)
            {
                int index;
                if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1 && index > 0)
                {
                    int prevSubmenuIndex = index - 1;
                    HcCtxMenuModel selectedMenu = this.SelectedMenu;
                    this.MenuList[index] = this.MenuList[prevSubmenuIndex];
                    this.MenuList[prevSubmenuIndex] = selectedMenu;
                    this.SelectedMenu = selectedMenu;
                }
            }
        }

        public ICommand MoveMenuUpCmd
        {
            get
            {
                if (this._moveMenuUpCmd == null)
                {
                    this._moveMenuUpCmd = new RelayCommand(this.MoveMenuUpAction);
                }
                return this._moveMenuUpCmd;
            }
        }

        private void MoveMenuDownAction(object param)
        {
            if (this.MenuList != null)
            {
                int index;
                if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1 && index < this.MenuList.Count - 1)
                {
                    int nextSubmenuIndex = index + 1;
                    HcCtxMenuModel selectedMenu = this.SelectedMenu;
                    this.MenuList[index] = this.MenuList[nextSubmenuIndex];
                    this.MenuList[nextSubmenuIndex] = selectedMenu;
                    this.SelectedMenu = selectedMenu;
                }
            }
        }

        public ICommand MoveMenuDownCmd
        {
            get
            {
                if (this._moveMenuDownCmd == null)
                {
                    this._moveMenuDownCmd = new RelayCommand(this.MoveMenuDownAction);
                }
                return this._moveMenuDownCmd;
            }
        }

        private void EditMenuPropAction(object param)
        {
            if (this.SelectedMenu != null)
            {
                ShellSubmenuEditor editor = new ShellSubmenuEditor(this.SelectedMenu);
                editor.Owner = this.Parent;
                editor.ShowDialog();
            }
            else
            {
                MessageBox.Show(this.Parent, "没有选择任何主菜单！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        public ICommand EditMenuPropCmd
        {
            get
            {
                if (this._editMenuPropCmd == null)
                {
                    this._editMenuPropCmd = new RelayCommand(this.EditMenuPropAction);
                }
                return this._editMenuPropCmd;
            }
        }

        private void ManuallyResetMenuList()
        {
            if (this.MenuList == null)
            {
                this.MenuList = new ObservableCollection<HcCtxMenuModel>();
            }
            else
            {
                this.MenuList.Clear();
            }
            HcCtxMenuModel menuCompute = new HcCtxMenuModel("计算所选对象的哈希值", true, MenuType.Compute);
            menuCompute.Submenus = new ObservableCollection<HcCtxMenuModel>
            {
                new HcCtxMenuModel("默认算法"),
            };
            HcCtxMenuModel menuCheckHash = new HcCtxMenuModel("作为哈希校验依据打开", true, MenuType.CheckHash);
            menuCheckHash.Submenus = new ObservableCollection<HcCtxMenuModel>
            {
                new HcCtxMenuModel("自动选择"),
            };
            foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
            {
                menuCompute.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
                menuCheckHash.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
            }
            this.MenuList.Add(menuCompute);
            this.MenuList.Add(menuCheckHash);
        }

        private string LoadMenuListFromJsonFile()
        {
            if (File.Exists(Settings.MenuConfigFile))
            {
                try
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    jsonSerializer.DefaultValueHandling = DefaultValueHandling.Populate;
                    using (StreamReader sr = new StreamReader(Settings.MenuConfigFile, menuEncoding))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        this.MenuList = jsonSerializer.Deserialize<ObservableCollection<HcCtxMenuModel>>(jsonTextReader);
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            return default(string);
        }

        private string CheckIfMenuListAllValid()
        {
            if (this.MenuList == null || !this.MenuList.Any())
            {
                this.ManuallyResetMenuList();
                return default(string);
            }
            foreach (HcCtxMenuModel hcCtxMenuModel in this.MenuList)
            {
                if (string.IsNullOrEmpty(hcCtxMenuModel.Title))
                {
                    return "主菜单列表中某项菜单的标题为空，请添加标题！";
                }
                if (hcCtxMenuModel.MenuType == MenuType.Unknown)
                {
                    return $"主菜单项【{hcCtxMenuModel.Title}】没有选择有效的菜单类型！";
                }
                if (hcCtxMenuModel.HasSubmenus)
                {
                    if (hcCtxMenuModel.Submenus == null || !hcCtxMenuModel.Submenus.Any())
                    {
                        return $"主菜单项【{hcCtxMenuModel.Title}】设置为\"有子菜单\"但未添加任何子菜单！";
                    }
                    foreach (HcCtxMenuModel submenu in hcCtxMenuModel.Submenus)
                    {
                        if (string.IsNullOrEmpty(submenu.Title))
                        {
                            return $"主菜单【{hcCtxMenuModel.Title}】的某项子菜单标题为空，请添加子菜单标题！";
                        }
                    }
                }
            }
            return default(string);
        }

        public string SaveMenuListToJsonFile()
        {
            try
            {
                if (!Directory.Exists(Settings.ActiveConfigDir))
                {
                    Directory.CreateDirectory(Settings.ActiveConfigDir);
                }
                if (this.CheckIfMenuListAllValid() is string checkMenuResult)
                {
                    return checkMenuResult;
                }
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.DefaultValueHandling = DefaultValueHandling.Ignore;
                using (StreamWriter sw = new StreamWriter(Settings.MenuConfigFile, false, menuEncoding))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jsonTextWriter, this.MenuList, typeof(ObservableCollection<HcCtxMenuModel>));
                    return null;
                }
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        public static GenericItemModel[] AvailableMenuTypes { get; } =
            new GenericItemModel[]
            {
                new GenericItemModel("计算哈希菜单", MenuType.Compute),
                new GenericItemModel("校验依据菜单", MenuType.CheckHash),
            };
    }
}
