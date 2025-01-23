using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HashCalculator
{
    internal class InvalidFileNameForegroundCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            if (values[0] is bool invalid && invalid)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else if (values[1] is Brush brush)
            {
                return brush;
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class ComparisonResultToForegroundCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush solidColorBrush = new SolidColorBrush(Colors.Transparent);
            if (value is CmpRes cmpResult)
            {
                switch (cmpResult)
                {
                    case CmpRes.Unrelated:
                        solidColorBrush.Color = (Color)ColorConverter.ConvertFromString("#B0B0B0");
                        break;
                    case CmpRes.Matched:
                        solidColorBrush.Color = (Color)ColorConverter.ConvertFromString("#228B22");
                        break;
                    case CmpRes.Mismatch:
                        solidColorBrush.Color = (Color)ColorConverter.ConvertFromString("#FF0000");
                        break;
                    case CmpRes.Uncertain:
                        solidColorBrush.Color = (Color)ColorConverter.ConvertFromString("#9C1CD8");
                        break;
                    case CmpRes.NoResult:
                    default:
                        break;
                }
            }
            return solidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class ComparisonResultToTextCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return "无关联";
                case CmpRes.Matched:
                    return "已匹配";
                case CmpRes.Mismatch:
                    return "不匹配";
                case CmpRes.Uncertain:
                    return "不确定";
                case CmpRes.NoResult:
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class ComparisonResultToIconFontTextCvt : IValueConverter
    {

        public string IconFontMatched { get; set; }

        public string IconFontMismatch { get; set; }

        public string IconFontUnrelated { get; set; }

        public string IconFontUncertain { get; set; }

        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return this.IconFontUnrelated;
                case CmpRes.Matched:
                    return this.IconFontMatched;
                case CmpRes.Mismatch:
                    return this.IconFontMismatch;
                case CmpRes.Uncertain:
                    return this.IconFontUncertain;
                case CmpRes.NoResult:
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class ComparisonResultToVisibilityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is CmpRes cmpResult && cmpResult != CmpRes.NoResult ?
                 Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class AlgoModelAndCmpResToVisibilityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            return values[0] != null && values[1] is CmpRes cmpResult && cmpResult != CmpRes.NoResult ?
                Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateRunningToVisiblityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashState state = (HashState)value;
            if (state != HashState.Running && state != HashState.Paused)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class SubBtnPauseToolTipCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashState)value == HashState.Running)
            {
                return "暂停...";
            }
            else
            {
                return "继续...";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BooleanToIconResourceCvt : IValueConverter
    {
        private readonly ResourceDictionary resourceDict = null;

        public bool State { get; set; }

        public string Resource { get; set; }

        public string OtherResource { get; set; }

        public BooleanToIconResourceCvt()
        {
            if (this.resourceDict == null)
            {
                this.resourceDict = new ResourceDictionary();
                this.resourceDict.Source = new Uri(
                    "/HashCalculator;component/Resources/ButtonIcons.xaml",
                    UriKind.Relative);
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.resourceDict != null)
            {
                return value is bool state && this.State == state ?
                    this.resourceDict[$"{this.Resource}DrawingImage"] :
                        this.resourceDict[$"{this.OtherResource}DrawingImage"];
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class HashStateToButtonTextCvt : IValueConverter
    {
        public HashState State { get; set; }

        public string Matched { get; set; }

        public string Mismatched { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is HashState state && state == this.State ? this.Matched : this.Mismatched;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateFinishedResultSucceededToVisibilityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (!(values[0] is HashState hashState) || hashState != HashState.Finished ||
                !(values[1] is HashResult hashResult) || hashResult != HashResult.Succeeded)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateNoStateFinishedResultNotSucceedToVisibilityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (!(values[0] is HashState hashState) ||
                (hashState != HashState.NoState && hashState != HashState.Finished))
            {
                return Visibility.Hidden;
            }
            if (!(values[1] is HashResult hashResult) || hashResult == HashResult.Succeeded)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateWaitingToVisiblityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashState)value != HashState.Waiting)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateNotRunningResultSucceededToVisibilityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (!(values[0] is HashState hashState) || hashState == HashState.Running ||
                hashState == HashState.Paused ||
                (values[1] is HashResult hashResult && hashResult == HashResult.Succeeded))
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class MainModelStateToBooleanCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((RunningState)value != RunningState.Started)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmdPanelCriticalControlsEnabledCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            return values[0] is RunningState state &&
                state != RunningState.Started &&
                values[1] is bool filterAndCmderEnabled &&
                filterAndCmderEnabled;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BooleanToVisibilityCvt : IValueConverter
    {
        public bool Boolean { get; set; }

        public Visibility Default { get; set; }

        public Visibility Fallback { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean && boolean == this.Boolean)
            {
                return this.Default;
            }
            return this.Fallback;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class LoadingImageVisiblityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((RunningState)value == RunningState.Started)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BytesToStrByOutputTypeCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            return Convert(values[0], values[1]);
        }

        internal static string Convert(object bytes, object output)
        {
            if (bytes is byte[] hashBytes && hashBytes.Any() && output is OutputType outputType)
            {
                switch (outputType)
                {
                    case OutputType.BASE64:
                        return CommonUtils.ToBase64String(hashBytes);
                    default:
                    case OutputType.BinaryUpper:
                        return CommonUtils.ToHexStringUpper(hashBytes);
                    case OutputType.BinaryLower:
                        return CommonUtils.ToHexStringLower(hashBytes);
                }
            }
            // 返回值可能被放置到剪贴板，所以不返回 null
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CopyModelsHashMenuEnabledCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList selectedItems)
            {
                if (!selectedItems.AnyItem())
                {
                    return false;
                }
                foreach (HashViewModel model in selectedItems.OfType<HashViewModel>())
                {
                    if (model.Result != HashResult.Succeeded)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class RadioExportCurrentAlgoCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportAlgo)value == ExportAlgo.Current;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportAlgo.Current : ExportAlgo.AllCalculated;
        }
    }

    internal class RadioExportAllCalculatedAlgosCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportAlgo)value == ExportAlgo.AllCalculated;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportAlgo.AllCalculated : ExportAlgo.Current;
        }
    }

    internal class BytesToIntuitiveFileSizeCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytesLength && bytesLength >= 0)
            {
                return CommonUtils.FileSizeCvt(bytesLength);
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class MultiLineTextToStrArrayCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[] textLineArray)
            {
                return string.Join("\n", textLineArray);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string multiLineText)
            {
                return multiLineText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim()).ToArray();
            }
            return default(string[]);
        }
    }

    internal class CmpColorToVisibilityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComparableColor color && color.Color != default(Color))
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmpColorToColorBrushCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComparableColor color && color.Color != default(Color))
            {
                return new SolidColorBrush(color.Color);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class MultiCmpColorToColorBrushCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is ComparableColor color1 && color1.Color != default(Color))
            {
                return new SolidColorBrush(color1.Color);
            }
            else if (values[1] is ComparableColor color2 && color2.Color != default(Color))
            {
                return new SolidColorBrush(color2.Color);
            }
            else
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmpColorOrBrushToColorBrushCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is ComparableColor color1 && color1.Color != default(Color))
            {
                return new SolidColorBrush(color1.Color);
            }
            else if (values[1] is Brush brush)
            {
                return brush;
            }
            else
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class ReverseBooleanValueCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    internal class CloneParameterArrayCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class SelectSmallerDoubleCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is double double1 && values[1] is double double2 && double1 * double2 != 0)
            {
                return Math.Min(double1, double2);
            }
            return double.NaN;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class RadiusFromSideLengthCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is double double1 && values[1] is double double2 && double1 * double2 != 0)
            {
                return new CornerRadius(Math.Min(double1, double2) / 2);
            }
            return new CornerRadius();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class EditSubmenusButtonEnabledCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            return values[0] != null && values[1] is bool result && result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class EditOriginalFileToTrueCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is EditFileOption option && option == EditFileOption.OriginalFile;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result && result)
            {
                return EditFileOption.OriginalFile;
            }
            return default(EditFileOption);
        }
    }

    internal class EditNewInSameLocationToTrueCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is EditFileOption option && option == EditFileOption.NewInSameLocation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result && result)
            {
                return EditFileOption.NewInSameLocation;
            }
            return default(EditFileOption);
        }
    }

    internal class EditNewInNewLocationToTrueCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is EditFileOption option && option == EditFileOption.NewInNewLocation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool result && result)
            {
                return EditFileOption.NewInNewLocation;
            }
            return default(EditFileOption);
        }
    }

    internal class DisplayHcmDataHashNameCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HcmData hcmData &&
                hcmData.DataReliable && hcmData.Name != null)
            {
                return $"算法: {hcmData.Name}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class DisplayHcmDataHashValueCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is HcmData hcmData &&
                values[1] is OutputType outputType &&
                hcmData.DataReliable && hcmData.Hash != null)
            {
                return $"哈希值: {BytesToStrByOutputTypeCvt.Convert(hcmData.Hash, outputType)}";
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class DisplayHcmDataErrorInfoCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HcmData hcmData)
            {
                if (!hcmData.DataReliable)
                {
                    return "哈希标记数据已损坏";
                }
                else if (hcmData.Name == null)
                {
                    return "哈希标记不含原文件哈希值";
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class AlgoInOutModelsToNumberCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is AlgoInOutModel[] models ? models.Length : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StringToValidIntValueCvt : IValueConverter
    {
        public int Default { get; set; }

        public int Min { get; set; } = int.MinValue;

        public int Max { get; set; } = int.MaxValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int number &&
                number >= this.Min && number <= this.Max)
            {
                return number.ToString();
            }
            return this.Default.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string intString &&
                int.TryParse(intString, out int number) &&
                number >= this.Min && number <= this.Max)
            {
                return number;
            }
            return this.Default;
        }
    }

    internal class StringToValidDoubleValueCvt : IValueConverter
    {
        public double Default { get; set; }

        public double Min { get; set; } = double.MinValue;

        public double Max { get; set; } = double.MaxValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double number &&
                number >= this.Min && number <= this.Max)
            {
                return number.ToString("f1");
            }
            return this.Default.ToString("f1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string doubleString &&
                double.TryParse(doubleString, out double number) &&
                number >= this.Min && number <= this.Max)
            {
                return number;
            }
            return this.Default;
        }
    }

    internal class ConfigurationLoadedLocationCvt : IValueConverter
    {
        public string Display { get; set; }

        public string Location { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string location &&
                !string.IsNullOrEmpty(this.Location) &&
                location.Equals(this.Location, StringComparison.OrdinalIgnoreCase))
            {
                return $"{this.Display}（最近加载）";
            }
            return this.Display;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BetweenConfigLocationAndBooleanCvt : IValueConverter
    {
        public bool Boolean { get; set; }

        public ConfigLocation Loccation { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConfigLocation location && location == this.Loccation)
            {
                return this.Boolean;
            }
            else
            {
                return !this.Boolean;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean && boolean == this.Boolean)
            {
                return this.Loccation;
            }
            else
            {
                return ConfigLocation.Unset;
            }
        }
    }

    internal class AssociatedMainAndFilterWndPositionCvt : IMultiValueConverter
    {
        /// <summary>
        /// true 是给窗口的左上角横坐标用的，否则是纵坐标用的
        /// </summary>
        public bool ForLeft { get; set; }

        /// <summary>
        /// true 是给主窗口用的，否则是给【筛选和操作】窗口用的
        /// </summary>
        public bool ForMainWnd { get; set; }

        public SettingsViewModel Settings { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double mainWndCoord && values[1] is double filterWndCoord)
            {
                return this.ForMainWnd ? mainWndCoord : filterWndCoord;
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is double filterOrMainWndCoord && this.Settings != null)
            {
                if (this.ForMainWnd)
                {
                    if (this.Settings.FilterAndCmderWndFollowsMainWnd)
                    {
                        double filterTopOrLeft = this.ForLeft ?
                            filterOrMainWndCoord + this.Settings.FilterPanelLeftRelToMain :
                                filterOrMainWndCoord + this.Settings.FilterPanelTopRelToMain;
                        return new object[] { filterOrMainWndCoord, filterTopOrLeft };
                    }
                    else
                    {
                        if (!this.ForLeft)
                        {
                            this.Settings.FilterPanelTopRelToMain = this.Settings.FilterAndCmderWndTop -
                                filterOrMainWndCoord;
                        }
                        else
                        {
                            this.Settings.FilterPanelLeftRelToMain = this.Settings.FilterAndCmderWndLeft -
                                filterOrMainWndCoord;
                        }
                        return new object[] { filterOrMainWndCoord, Binding.DoNothing };
                    }
                }
                else
                {
                    if (this.Settings.FilterAndCmderWndFollowsMainWnd)
                    {
                        if (!this.ForLeft)
                        {
                            this.Settings.FilterPanelTopRelToMain = filterOrMainWndCoord - this.Settings.MainWindowTop;
                        }
                        else
                        {
                            this.Settings.FilterPanelLeftRelToMain = filterOrMainWndCoord - this.Settings.MainWindowLeft;
                        }
                    }
                    return new object[] { Binding.DoNothing, filterOrMainWndCoord };
                }
            }
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

    internal class StateAndSelectionWayToMonitoring : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            return values[0] is RunningState state && values[1] is bool selectedByCheckbox &&
                state != RunningState.Started && !selectedByCheckbox;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class RenameFileMethodToBooleanCvt : IValueConverter
    {
        public RenameFileMethod Method { get; set; }

        public bool Boolean { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RenameFileMethod method && method == this.Method)
            {
                return this.Boolean;
            }
            else
            {
                return !this.Boolean;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean && boolean == this.Boolean)
            {
                return this.Method;
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    }

    internal class BooleanAndSolidBrushToSolidBrushMultiCvt : IMultiValueConverter
    {
        public Color Default { get; set; } = Colors.Gray;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values?.Length == 2);
            if (values[0] is bool isSelected && isSelected && values[1] is Brush foreground)
            {
                return foreground;
            }
            return new SolidColorBrush(this.Default);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
