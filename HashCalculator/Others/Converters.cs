using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HashCalculator
{
    internal class InvalidFileNameForegroundCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool invalidFileName && invalidFileName)
            {
                return "#FF0000";
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmpResForegroundCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (!(bool)values[0])
            {
                return new SolidColorBrush(Colors.Transparent);
            }
            if (values[1] is CmpRes cmpResult)
            {
                switch (cmpResult)
                {
                    case CmpRes.Unrelated:
                        return new SolidColorBrush(Colors.Black);
                    case CmpRes.Matched:
                        return new SolidColorBrush(Colors.White);
                    case CmpRes.Mismatch:
                        return new SolidColorBrush(Colors.White);
                    case CmpRes.Uncertain:
                        return new SolidColorBrush(Colors.White);
                    case CmpRes.NoResult:
                    default:
                        return new SolidColorBrush(Colors.Transparent);
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmpResBackgroundCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            switch ((CmpRes)value)
            {
                case CmpRes.Unrelated:
                    return "#64888888";
                case CmpRes.Matched:
                    return "ForestGreen";
                case CmpRes.Mismatch:
                    return "Red";
                case CmpRes.Uncertain:
                    return "Black";
                case CmpRes.NoResult:
                default:
                    return "Transparent";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.Unrelated; // 此处未使用，只返回默认值
        }
    }

    internal class CmpResTextCvt : IValueConverter
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
                    return "未校验";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class CmpResBorderBrushCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((CmpRes)value == CmpRes.NoResult)
            {
                return new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0091FF"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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

    internal class SubBtnPauseImageSrcCvt : IValueConverter
    {
        private readonly BitmapImage paused =
            new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));
        private readonly BitmapImage notPaused =
            new BitmapImage(new Uri("/Images/continue.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashState)value == HashState.Running)
            {
                return this.paused;
            }
            else
            {
                return this.notPaused;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class StateFinishedResultSucceededToVisiblityCvt : IMultiValueConverter
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

    internal class StateFinishedResultNotSucceedToVisiblityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values != null && values.Length == 2);
            if (!(values[0] is HashState hashState) || hashState != HashState.Finished)
            {
                return Visibility.Hidden;
            }
            if (!(values[1] is HashResult hashResult) ||
                (hashResult != HashResult.Canceled && hashResult != HashResult.Failed))
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

    internal class StateNotRunningResultSucceededToVisiblityCvt : IMultiValueConverter
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
            if ((QueueState)value != QueueState.Started)
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
            Debug.Assert(values != null && values.Length == 2);
            if (values[0] is QueueState state && state == QueueState.Started)
            {
                return false;
            }
            if (values[1] is bool filterOrCmderEnabled && !filterOrCmderEnabled)
            {
                return false;
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class NoColumnCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
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

    internal class ShowColumnCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(bool)value)
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

    internal class ReverseNoColumnCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(bool)value)
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

    internal class BtnExportEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/export_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/export_32_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnNewLineEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/new_line_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/new_line_32_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnRefreshEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/refresh_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/refresh_32_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnForceRefreshEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/refresh_force_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/refresh_force_32_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnTopmostEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/topmost_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/topmost_32_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnRefreshFiltersEnabledImgSrcCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/refresh_64.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/refresh_64_gray.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((bool)value)
            {
                case true:
                    return enabled;
                default:
                    return disabled;
            }
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
            if ((QueueState)value == QueueState.Started)
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

        internal static string Convert(object bytes, object outputType)
        {
            if (!(bytes is byte[] hashBytes) || !hashBytes.Any())
            {
                return string.Empty;
            }
            switch ((OutputType)outputType)
            {
                default:
                case OutputType.BinaryUpper:
                    return CommonUtils.ToHexStringUpper(hashBytes);
                case OutputType.BinaryLower:
                    return CommonUtils.ToHexStringLower(hashBytes);
                case OutputType.BASE64:
                    return System.Convert.ToBase64String(hashBytes);
                // 返回值可能被复制所以不返回 null，上同
                case OutputType.Unknown:
                    return string.Empty;
            }
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

    internal class PlaceHolderTextVisibilityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(value as string))
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

    internal class RadioExportAsTxtFileCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportType)value == ExportType.TxtFile;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportType.TxtFile : ExportType.HcbFile;
        }
    }

    internal class RadioExportAsHcbFileCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportType)value == ExportType.HcbFile;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportType.HcbFile : ExportType.TxtFile;
        }
    }

    internal class RadioExportCurrentAlgoCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportAlgos)value == ExportAlgos.Current;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportAlgos.Current : ExportAlgos.AllCalculated;
        }
    }

    internal class RadioExportAllCalculatedAlgosCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ExportAlgos)value == ExportAlgos.AllCalculated;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? ExportAlgos.AllCalculated : ExportAlgos.Current;
        }
    }

    internal class BytesToIntuitiveFileSizeCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytesLength)
            {
                return CommonUtils.FileSizeCvt(bytesLength);
            }
            return "未知大小";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CommandPanelTopCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.All(i => i is double))
            {
                return (double)values[0] + (double)values[1];
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, (double)value - Settings.Current.MainWindowTop };
        }
    }

    internal class CommandPanelLeftCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.All(i => i is double))
            {
                return (double)values[0] + (double)values[1];
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, (double)value - Settings.Current.MainWindowLeft };
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

    internal class CmpColorToSolidColorBrushCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ComparableColor color && color.Color != default(Color))
            {
                return new SolidColorBrush(color.Color);
            }
            else
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class MultiCmpColorToSolidColorBrushCvt : IMultiValueConverter
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

    internal class ReverseBooleanValueCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
}
