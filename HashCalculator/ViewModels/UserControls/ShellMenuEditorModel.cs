using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using HashCalculator.ViewModels.Pages;
using HashCalculator.Views.Windows;

namespace HashCalculator.ViewModels.UserControls;

public class ShellMenuEditorModel : BaseViewModel
{
    private HcCtxMenuModel _selectedMenu;
    private ObservableCollection<HcCtxMenuModel> _menuList;
    private static readonly Encoding menuEncoding = Encoding.Unicode;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };
    private RelayCommand _addMenuCmd;
    private RelayCommand _deleteMenuCmd;
    private RelayCommand _moveMenuUpCmd;
    private RelayCommand _moveMenuDownCmd;

    public ShellMenuEditorModel()
    {
        if (this.LoadMenuListFromJsonFile() is string reason)
        {
            NotificationSender.ShowMessageBox(
                MainWindow.Current, "警告", $"载入快捷菜单配置文件失败：{reason}");
        }
    }

    public ObservableCollection<HcCtxMenuModel> MenuList
    {
        get => this._menuList;
        set => this.SetPropNotify(ref this._menuList, value);
    }

    public HcCtxMenuModel SelectedMenu
    {
        get => this._selectedMenu;
        set => this.SetPropNotify(ref this._selectedMenu, value);
    }

    private void AddMenuAction(object param)
    {
        HcCtxMenuModel hcCtxMenuModel = new HcCtxMenuModel();
        this.MenuList ??= new ObservableCollection<HcCtxMenuModel>();
        this.MenuList.Add(hcCtxMenuModel);
        this.SelectedMenu = hcCtxMenuModel;
    }

    public ICommand AddMenuListCmd
    {
        get
        {
            this._addMenuCmd ??= new RelayCommand(this.AddMenuAction);
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
                NotificationSender.ShowMessageBox(MainWindow.Current, "提示", "没有选择任何主菜单！");
            }
        }
    }

    public ICommand DeleteMenuCmd
    {
        get
        {
            this._deleteMenuCmd ??= new RelayCommand(this.DeleteMenuAction);
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
            this._moveMenuUpCmd ??= new RelayCommand(this.MoveMenuUpAction);
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
            this._moveMenuDownCmd ??= new RelayCommand(this.MoveMenuDownAction);
            return this._moveMenuDownCmd;
        }
    }

    public void ManuallyResetMenuList()
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
        HcCtxMenuModel menuCheckHash = new HcCtxMenuModel("作为哈希校验信息打开", true, MenuType.CheckHash);
        menuCheckHash.Submenus = new ObservableCollection<HcCtxMenuModel>
        {
            new HcCtxMenuModel("自动选择"),
        };
        foreach (AlgoInOutModel model in AlgorithmsModel.ProvidedAlgos)
        {
            menuCompute.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
            menuCheckHash.Submenus.Add(new HcCtxMenuModel(model.AlgoName, model.AlgoType.ToString()));
        }
        this.MenuList.Add(menuCompute);
        this.MenuList.Add(menuCheckHash);
    }

    private string LoadMenuListFromJsonFile()
    {
        if (File.Exists(Settings.ConfigInfo.MenuConfigFile))
        {
            try
            {
                using (StreamReader sr = new StreamReader(Settings.ConfigInfo.MenuConfigFile, menuEncoding))
                {
                    this.MenuList = JsonSerializer.Deserialize<ObservableCollection<HcCtxMenuModel>>(
                        sr.ReadToEnd(), JsonOptions);
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
            if (!Directory.Exists(Settings.ConfigInfo.ActiveConfigDir))
            {
                Directory.CreateDirectory(Settings.ConfigInfo.ActiveConfigDir);
            }
            if (this.CheckIfMenuListAllValid() is string checkMenuResult)
            {
                return checkMenuResult;
            }
            using (StreamWriter sw = new StreamWriter(Settings.ConfigInfo.MenuConfigFile, false, menuEncoding))
            {
                sw.Write(JsonSerializer.Serialize(this.MenuList, JsonOptions));
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
            new GenericItemModel("校验信息菜单", MenuType.CheckHash),
        };
}
