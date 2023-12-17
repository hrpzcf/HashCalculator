﻿using System;
using System.Collections.Generic;
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
        private RelayCommand _resetMenusCmd;
        private RelayCommand _saveMenuListCmd;
        private RelayCommand _addMenuCmd;
        private RelayCommand _deleteMenuCmd;
        private RelayCommand _moveMenuUpCmd;
        private RelayCommand _moveMenuDownCmd;
        private RelayCommand _editMenuPropCmd;
        private ObservableCollection<HcCtxMenuModel> _menuList =
            new ObservableCollection<HcCtxMenuModel>();

        public ShellMenuEditor Parent { get; }

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
            int index;
            if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1)
            {
                this.MenuList.Remove(this.SelectedMenu);
                if (index < this.MenuList.Count)
                {
                    this.SelectedMenu = this.MenuList[index];
                }
                else if (index - 1 >= 0)
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
            int index;
            if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1 && index > 0)
            {
                int prevSubenuIndex = index - 1;
                HcCtxMenuModel selectedMenu = this.SelectedMenu;
                this.MenuList[index] = this.MenuList[prevSubenuIndex];
                this.MenuList[prevSubenuIndex] = selectedMenu;
                this.SelectedMenu = selectedMenu;
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
            int index;
            if ((index = this.MenuList.IndexOf(this.SelectedMenu)) != -1 && index < this.MenuList.Count - 1)
            {
                int nextSubenuIndex = index + 1;
                HcCtxMenuModel selectedMenu = this.SelectedMenu;
                this.MenuList[index] = this.MenuList[nextSubenuIndex];
                this.MenuList[nextSubenuIndex] = selectedMenu;
                this.SelectedMenu = selectedMenu;
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

        public ShellMenuEditorModel(ShellMenuEditor parent)
        {
            this.Parent = parent;
            this.LoadMenuListFromJsonFile();
            this.InitAvailableAlgTypes();
        }

        private void InitAvailableAlgTypes()
        {
            AvailableAlgoTypes.AddRange(
                AlgosPanelModel.ProvidedAlgos.Select(i => new ControlItem(i.AlgoName, i.AlgoType.ToString())));
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
                new HcCtxMenuModel("默认算法", string.Empty),
            };
            HcCtxMenuModel menuCheckHash = new HcCtxMenuModel("作为哈希校验依据打开", true, MenuType.CheckHash);
            menuCheckHash.Submenus = new ObservableCollection<HcCtxMenuModel>
            {
                new HcCtxMenuModel("自动选择", string.Empty),
            };
            foreach (AlgoInOutModel model in AlgosPanelModel.ProvidedAlgos)
            {
                menuCompute.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
                menuCheckHash.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
            }
            this.MenuList.Add(menuCompute);
            this.MenuList.Add(menuCheckHash);
        }

        private bool LoadMenuListFromJsonFile()
        {
            if (File.Exists(Settings.MenuConfigFile))
            {
                try
                {
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                    using (StreamReader sr = new StreamReader(Settings.MenuConfigFile, Encoding.Default))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        this.MenuList = jsonSerializer.Deserialize<ObservableCollection<HcCtxMenuModel>>(jsonTextReader);
                        if (this.MenuList == null || !this.MenuList.Any())
                        {
                            this.ManuallyResetMenuList();
                        }
                        return true;
                    }
                }
                catch (Exception) { }
            }
            return false;
        }

        private string CheckIfMenuListAllValid()
        {
            if (!this.MenuList.Any(i => i.MenuType == MenuType.Compute) ||
                    !this.MenuList.Any(i => i.MenuType == MenuType.CheckHash))
            {
                return "主菜单列表为空或缺失两种类型菜单其中的一种。";
            }
            foreach (HcCtxMenuModel hcCtxMenuModel in this.MenuList)
            {
                if (string.IsNullOrEmpty(hcCtxMenuModel.Title))
                {
                    return "主菜单列表中某项菜单的标题为空！";
                }
                if (hcCtxMenuModel.MenuType == MenuType.Unknown)
                {
                    return $"主菜单项<{hcCtxMenuModel.Title}>没有选择有效的菜单类型！";
                }
                if (hcCtxMenuModel.HasSubmenus)
                {
                    hcCtxMenuModel.AlgType = null;
                    if (hcCtxMenuModel.Submenus == null || !hcCtxMenuModel.Submenus.Any())
                    {
                        return $"主菜单项<{hcCtxMenuModel.Title}>设置为\"有子菜单\"但未添加任何子菜单！";
                    }
                    foreach (HcCtxMenuModel submenu in hcCtxMenuModel.Submenus)
                    {
                        if (string.IsNullOrEmpty(submenu.Title))
                        {
                            return $"主菜单<{hcCtxMenuModel.Title}>的某项子菜单标题为空！";
                        }
                        if (submenu.AlgType == null)
                        {
                            return $"主菜单<{hcCtxMenuModel.Title}>的某项子菜单没有设置算法！";
                        }
                    }
                }
                else
                {
                    hcCtxMenuModel.Submenus = null;
                    if (hcCtxMenuModel.AlgType == null)
                    {
                        return $"主菜单项<{hcCtxMenuModel.Title}>设置为\"没有子菜单\"但未指定任何算法！";
                    }
                }
            }
            return default(string);
        }

        private string SaveMenuListToJsonFile()
        {
            try
            {
                if (!Directory.Exists(Settings.ConfigDir.FullName))
                {
                    Settings.ConfigDir.Create();
                }
                string checkMenuResult = this.CheckIfMenuListAllValid();
                if (!string.IsNullOrEmpty(checkMenuResult))
                {
                    return checkMenuResult;
                }
                JsonSerializer jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                using (StreamWriter sw = new StreamWriter(Settings.MenuConfigFile, false, Encoding.Default))
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(sw))
                {
                    if (this.MenuList == null || !this.MenuList.Any())
                    {
                        this.ManuallyResetMenuList();
                    }
                    jsonSerializer.Serialize(jsonTextWriter, this.MenuList, typeof(ObservableCollection<HcCtxMenuModel>));
                    return default(string);
                }
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        public static ControlItem[] AvailableMenuTypes { get; } =
            new ControlItem[]
            {
                new ControlItem("计算哈希菜单", MenuType.Compute),
                new ControlItem("校验依据菜单", MenuType.CheckHash),
            };

        public static List<ControlItem> AvailableAlgoTypes { get; } =
            new List<ControlItem>
            {
                new ControlItem("不指定算法", string.Empty),
            };
    }
}
