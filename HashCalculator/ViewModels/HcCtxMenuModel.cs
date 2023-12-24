using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace HashCalculator
{
    public class HcCtxMenuModel : NotifiableModel
    {
        private const char sep = ',';
        private string _title;
        private bool _hasSubmenus;
        private ObservableCollection<HcCtxMenuModel> _submenus;
        private GenericItemModel _currentAlgoType;
        private HcCtxMenuModel _selectedSubmenu;
        private RelayCommand _addSubmenuCmd;
        private RelayCommand _deleteSubmenuCmd;
        private RelayCommand _moveSubmenuUpCmd;
        private RelayCommand _moveSubmenuDownCmd;

        public HcCtxMenuModel()
        {
            this.AvailableAlgTypes = GenericItemModelsFromProvidedAlgos();
            this.CurrentAlgoType = this.AvailableAlgTypes[0];
        }

        public HcCtxMenuModel(string title) : this()
        {
            this.Title = title;
            this.CurrentAlgoType = this.AvailableAlgTypes[0];
        }

        public HcCtxMenuModel(string title, string algType) : this()
        {
            this.Title = title;
            this.AlgTypes = algType;
            foreach (GenericItemModel item in this.AvailableAlgTypes)
            {
                if (algType.Equals((string)item.ItemValue, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    this.CurrentAlgoType = item;
                    break;
                }
            }
        }

        public HcCtxMenuModel(string title, bool hasSubmenus, MenuType menuType) : this()
        {
            this.Title = title;
            this.HasSubmenus = hasSubmenus;
            this.MenuType = menuType;
        }

        public MenuType MenuType { get; set; }

        public string Title
        {
            get => this._title;
            set => this.SetPropNotify(ref this._title, value);
        }

        public string AlgTypes { get; set; }

        public ObservableCollection<HcCtxMenuModel> Submenus
        {
            get => this._submenus;
            set => this.SetPropNotify(ref this._submenus, value);
        }

        public string ShortCutKey { get; set; }

        [JsonIgnore]
        public bool HasSubmenus
        {
            get => this._hasSubmenus;
            set => this.SetPropNotify(ref this._hasSubmenus, value);
        }

        [JsonIgnore]
        public GenericItemModel CurrentAlgoType
        {
            get => this._currentAlgoType;
            set => this.SetPropNotify(ref this._currentAlgoType, value);
        }

        [JsonIgnore]
        public HcCtxMenuModel SelectedSubmenu
        {
            get => this._selectedSubmenu;
            set => this.SetPropNotify(ref this._selectedSubmenu, value);
        }

        [JsonIgnore]
        public GenericItemModel[] AvailableAlgTypes { get; }

        private void AddSubmenuAction(object param)
        {
            HcCtxMenuModel hcCtxMenuModel = new HcCtxMenuModel();
            if (this.Submenus == null)
            {
                this.Submenus = new ObservableCollection<HcCtxMenuModel>();
            }
            this.Submenus.Add(hcCtxMenuModel);
            this.SelectedSubmenu = hcCtxMenuModel;
        }

        [JsonIgnore]
        public ICommand AddSubmenuCmd
        {
            get
            {
                if (this._addSubmenuCmd == null)
                {
                    this._addSubmenuCmd = new RelayCommand(this.AddSubmenuAction);
                }
                return this._addSubmenuCmd;
            }
        }

        private void DeleteSubmenuAction(object param)
        {
            if (this.Submenus != null)
            {
                int index;
                if ((index = this.Submenus.IndexOf(this.SelectedSubmenu)) != -1)
                {
                    this.Submenus.RemoveAt(index);
                    if (index < this.Submenus.Count)
                    {
                        this.SelectedSubmenu = this.Submenus[index];
                    }
                    else if (index > 0)
                    {
                        this.SelectedSubmenu = this.Submenus[index - 1];
                    }
                }
                else
                {
                    MessageBox.Show(ShellMenuEditor.This, "没有选择任何菜单项！", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        [JsonIgnore]
        public ICommand DeleteSubmenuCmd
        {
            get
            {
                if (this._deleteSubmenuCmd == null)
                {
                    this._deleteSubmenuCmd = new RelayCommand(this.DeleteSubmenuAction);
                }
                return this._deleteSubmenuCmd;
            }
        }

        private void MoveSubmenuUpAction(object param)
        {
            if (this.Submenus != null)
            {
                int index;
                if ((index = this.Submenus.IndexOf(this.SelectedSubmenu)) != -1 && index > 0)
                {
                    int prevSubenuIndex = index - 1;
                    HcCtxMenuModel selectedMenu = this.SelectedSubmenu;
                    this.Submenus[index] = this.Submenus[prevSubenuIndex];
                    this.Submenus[prevSubenuIndex] = selectedMenu;
                    this.SelectedSubmenu = selectedMenu;
                }
            }
        }

        [JsonIgnore]
        public ICommand MoveSubmenuUpCmd
        {
            get
            {
                if (this._moveSubmenuUpCmd == null)
                {
                    this._moveSubmenuUpCmd = new RelayCommand(this.MoveSubmenuUpAction);
                }
                return this._moveSubmenuUpCmd;
            }
        }

        private void MoveSubmenuDownAction(object param)
        {
            if (this.Submenus != null)
            {
                int index;
                if ((index = this.Submenus.IndexOf(this.SelectedSubmenu)) != -1 && index < this.Submenus.Count - 1)
                {
                    int nextSubenuIndex = index + 1;
                    HcCtxMenuModel selectedMenu = this.SelectedSubmenu;
                    this.Submenus[index] = this.Submenus[nextSubenuIndex];
                    this.Submenus[nextSubenuIndex] = selectedMenu;
                    this.SelectedSubmenu = selectedMenu;
                }
            }
        }

        [JsonIgnore]
        public ICommand MoveSubmenuDownCmd
        {
            get
            {
                if (this._moveSubmenuDownCmd == null)
                {
                    this._moveSubmenuDownCmd = new RelayCommand(this.MoveSubmenuDownAction);
                }
                return this._moveSubmenuDownCmd;
            }
        }

        [OnDeserialized]
        internal void OnHcCtxMenuModelDeserialized(StreamingContext context)
        {
            this.HasSubmenus = this.Submenus != null;
            if (!this.HasSubmenus)
            {
                if (!string.IsNullOrEmpty(this.AlgTypes))
                {
                    string[] algTypeList = this.AlgTypes.Split(sep);
                    foreach (GenericItemModel model in this.AvailableAlgTypes)
                    {
                        if (algTypeList.Contains((string)model.ItemValue, StringComparer.OrdinalIgnoreCase))
                        {
                            model.Selected = true;
                            if (this.CurrentAlgoType == null || !this.CurrentAlgoType.Selected)
                            {
                                this.CurrentAlgoType = model;
                            }
                        }
                    }
                }
            }
        }

        [OnSerializing]
        internal void OnHcCtxMenuModelSerializing(StreamingContext context)
        {
            if (this.HasSubmenus)
            {
                this.AlgTypes = null;
            }
            else
            {
                this.Submenus = null;
                StringBuilder jsonValueStringBuilder = new StringBuilder();
                foreach (GenericItemModel item in this.AvailableAlgTypes)
                {
                    if (item.Selected)
                    {
                        jsonValueStringBuilder.Append((string)item.ItemValue);
                        jsonValueStringBuilder.Append(sep);
                    }
                }
                if (jsonValueStringBuilder.Length > 0 && jsonValueStringBuilder[jsonValueStringBuilder.Length - 1] == sep)
                {
                    jsonValueStringBuilder.Remove(jsonValueStringBuilder.Length - 1, 1);
                }
                this.AlgTypes = jsonValueStringBuilder.ToString();
            }
        }

        public static GenericItemModel[] GenericItemModelsFromProvidedAlgos()
        {
            return AlgosPanelModel.ProvidedAlgos.Select(
                i => new GenericItemModel(i.AlgoName, i.AlgoType.ToString())).ToArray();
        }
    }
}
