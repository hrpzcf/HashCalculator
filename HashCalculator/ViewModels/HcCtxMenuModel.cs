using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace HashCalculator
{
    public class HcCtxMenuModel : NotifiableModel
    {
        private string _title;
        private string _algType;
        private bool _hasSubmenus;
        private ObservableCollection<HcCtxMenuModel> _submenus;
        private HcCtxMenuModel _selectedSubmenu;
        private RelayCommand _addSubmenuCmd;
        private RelayCommand _deleteSubmenuCmd;
        private RelayCommand _moveSubmenuUpCmd;
        private RelayCommand _moveSubmenuDownCmd;

        public string Title
        {
            get => this._title;
            set => this.SetPropNotify(ref this._title, value);
        }

        public string AlgType
        {
            get => this._algType;
            set => this.SetPropNotify(ref this._algType, value);
        }

        public MenuType MenuType { get; set; }

        public string ShortCutKey { get; set; }

        public bool HasSubmenus
        {
            get => this._hasSubmenus;
            set => this.SetPropNotify(ref this._hasSubmenus, value);
        }

        public ObservableCollection<HcCtxMenuModel> Submenus
        {
            get => this._submenus;
            set => this.SetPropNotify(ref this._submenus, value);
        }

        [JsonIgnore]
        public HcCtxMenuModel SelectedSubmenu
        {
            get => this._selectedSubmenu;
            set => this.SetPropNotify(ref this._selectedSubmenu, value);
        }

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
            int index;
            if ((index = this.Submenus.IndexOf(this.SelectedSubmenu)) != -1)
            {
                this.Submenus.Remove(this.SelectedSubmenu);
                if (index < this.Submenus.Count)
                {
                    this.SelectedSubmenu = this.Submenus[index];
                }
                else if (index - 1 >= 0)
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

        public HcCtxMenuModel() { }

        public HcCtxMenuModel(string title, string algType)
        {
            this.Title = title;
            this.AlgType = algType;
        }

        public HcCtxMenuModel(string title, bool hasSubmenus, MenuType menuType)
        {
            this.Title = title;
            this.HasSubmenus = hasSubmenus;
            this.MenuType = menuType;
        }
    }
}
