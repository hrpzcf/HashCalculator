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
    internal class CmpResForegroundCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
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

    //internal class AlgoTypeBackgroundCvt : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is AlgoInOutModel model)
    //        {
    //            switch (model.Type)
    //            {
    //                case AlgoType.SHA1:
    //                    return "#64FF0000";
    //                case AlgoType.SHA224:
    //                    return "#64ff5900";
    //                case AlgoType.SHA256:
    //                    return "#64ff8900";
    //                case AlgoType.SHA384:
    //                    return "#64ffaa00";
    //                case AlgoType.SHA512:
    //                    return "#64ffc600";
    //                case AlgoType.SHA3_224:
    //                    return "#64ffe100";
    //                case AlgoType.SHA3_256:
    //                    return "#64ffff00";
    //                case AlgoType.SHA3_384:
    //                    return "#64bdf400";
    //                case AlgoType.SHA3_512:
    //                    return "#647ce700";
    //                case AlgoType.MD5:
    //                    return "#6400cc00";
    //                case AlgoType.BLAKE2S:
    //                    return "#642618b1";
    //                case AlgoType.BLAKE2B:
    //                    return "#641240ab";
    //                case AlgoType.BLAKE3:
    //                    return "#647109aa";
    //                case AlgoType.WHIRLPOOL:
    //                    return "#6400a876";
    //                default:
    //                    return "#64A0A0A0";
    //            }
    //        }
    //        else
    //        {
    //            return "Transparent";
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return AlgoType.SHA256; // 此处未使用，只返回默认值
    //    }
    //}

    internal class SubBtnVisiblityRunningCvt : IValueConverter
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

    internal class SubCtrlVisiblitySucceededCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
            if ((HashState)values[0] != HashState.Finished ||
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

    internal class SubCtrlVisiblityNotSucceedCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
            if ((HashState)values[0] != HashState.Finished)
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

    internal class SubBtnVisiblityWaitingCvt : IValueConverter
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

    internal class MenuCopyHashEnabledCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashResult)value == HashResult.Succeeded)
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

    internal class TaskMessageCtrlVisiblityCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
            HashState hashState = (HashState)values[0];
            if (hashState == HashState.Running || hashState == HashState.Paused
                || (values[1] is HashResult hashResult && hashResult == HashResult.Succeeded))
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

    internal class MainModelStateToBoolCvt : IValueConverter
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
            Debug.Assert(values.Length == 2);
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

    internal class BtnExportEnabledImgCvt : IValueConverter
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

    internal class BtnNewLineEnabledImgCvt : IValueConverter
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

    internal class BtnRefreshEnabledImgCvt : IValueConverter
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

    internal class BtnRefreshForceEnabledImgCvt : IValueConverter
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

    internal class BtnTopmostEnabledImgCvt : IValueConverter
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

    internal class BtnRefreshFilterEnabledImgCvt : IValueConverter
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
            Debug.Assert(values.Length == 2);
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

    internal class HashDetailsFileSizeExCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytesLength)
            {
                return $"{CommonUtils.FileSizeCvt(bytesLength)} ({bytesLength} 字节)";
            }
            return "未知字节数";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class ToFriendlyFileSizeCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytesLength)
            {
                return CommonUtils.FileSizeCvt(bytesLength);
            }
            return "未知字节数";
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

    internal class GroupIdToSolidColorBrushCvt : IValueConverter
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

    internal class ReverseBoolValueCvt : IValueConverter
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
}
