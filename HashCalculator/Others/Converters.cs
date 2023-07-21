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
    internal class CmpResFgCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
            if (!(bool)values[0])
            {
                return new SolidColorBrush(Colors.Transparent);
            }
            switch ((CmpRes)values[1])
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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class CmpResBgCvt : IValueConverter
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
                    return string.Empty;
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

    internal class AlgoTypeBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((AlgoType)value)
            {
                case AlgoType.SHA1:
                    return "#64FF0000";
                case AlgoType.SHA224:
                    return "#64ff5900";
                case AlgoType.SHA256:
                    return "#64ff8900";
                case AlgoType.SHA384:
                    return "#64ffaa00";
                case AlgoType.SHA512:
                    return "#64ffc600";
                case AlgoType.SHA3_224:
                    return "#64ffe100";
                case AlgoType.SHA3_256:
                    return "#64ffff00";
                case AlgoType.SHA3_384:
                    return "#64bdf400";
                case AlgoType.SHA3_512:
                    return "#647ce700";
                case AlgoType.MD5:
                    return "#6400cc00";
                case AlgoType.BLAKE2S:
                    return "#642618b1";
                case AlgoType.BLAKE2B:
                    return "#641240ab";
                case AlgoType.BLAKE3:
                    return "#647109aa";
                case AlgoType.WHIRLPOOL:
                    return "#6400a876";
                default:
                    return "#64A0A0A0";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AlgoType.SHA256; // 此处未使用，只返回默认值
        }
    }

    internal class AlgoTypeNameCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AlgoMap.GetAlgoName((AlgoType)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

    internal class FinishedVisiblityCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashState state = (HashState)value;
            if (state == HashState.Running || state == HashState.Paused)
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

    internal class SubBtnVisiblitySucceededCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashResult)value != HashResult.Succeeded)
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

    internal class SubBtnVisiblityUnsuccessfulCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashResult hashResult = (HashResult)value;
            if (hashResult != HashResult.Canceled && hashResult != HashResult.Failed)
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

    internal class ButtonEnabledCvt : IValueConverter
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

    internal class SubProgressWidthCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AlgoType algoType = (AlgoType)value;
            switch (algoType)
            {
                default:
                case AlgoType.MD5:
                    return 220.0;
                case AlgoType.SHA1:
                    return 280.0;
                case AlgoType.SHA224:
                case AlgoType.SHA3_224:
                    return 380.0;
                case AlgoType.BLAKE2S:
                case AlgoType.BLAKE3:
                case AlgoType.SHA256:
                case AlgoType.SHA3_256:
                    return 440.0;
                case AlgoType.SHA384:
                case AlgoType.SHA3_384:
                    return 660.0;
                case AlgoType.BLAKE2B:
                case AlgoType.SHA512:
                case AlgoType.SHA3_512:
                case AlgoType.WHIRLPOOL:
                    return 860.0;
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

    internal class HashBytesOutputTypeCvt : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.Assert(values.Length == 2);
            return Convert(values[0], values[1]);
        }

        internal static object Convert(object bytes, object outputType)
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
                case OutputType.Unknown:
                    return string.Empty;  // 返回值可能被复制所以不返回 null，上同
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

    internal class HashOrPathPlaceHolderVisibCvt : IValueConverter
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
}
