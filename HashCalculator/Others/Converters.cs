﻿using System;
using System.Diagnostics;
using System.Globalization;
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

    internal class CmpResBorderCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "0";
            }
            else
            {
                return "3";
            }
        }

        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            return CmpRes.NoResult; // 此处未使用，只返回默认值
        }
    }

    internal class AlgoTypeBgCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((AlgoType)value)
            {
                case AlgoType.SHA256:
                    return "#640066FF";
                case AlgoType.SHA1:
                    return "#64FF0071";
                case AlgoType.SHA224:
                    return "#64331772";
                case AlgoType.SHA384:
                    return "#64FFBB33";
                case AlgoType.SHA512:
                    return "#64008B73";
                case AlgoType.MD5:
                    return "#64799B00";
                default:
                    return "#64FF0000";
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
            if ((AlgoType)value == AlgoType.Unknown)
            {
                return "待定";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class VisibRunningCvt : IValueConverter
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

    internal class PauseBtnTextCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashState state = (HashState)value;
            if (state == HashState.Running)
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

    internal class PauseBtnImgsrcCvt : IValueConverter
    {
        private readonly BitmapImage paused =
            new BitmapImage(new Uri("/Images/pause.png", UriKind.Relative));
        private readonly BitmapImage noPaused =
            new BitmapImage(new Uri("/Images/continue.png", UriKind.Relative));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HashState state = (HashState)value;
            if (state == HashState.Running)
            {
                return this.paused;
            }
            else
            {
                return this.noPaused;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class VisibNotCalcCvt : IValueConverter
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

    internal class VisibWaitingCvt : IValueConverter
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

    internal class VisibSucceededCvt : IValueConverter
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

    internal class VisibCanceledCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((HashResult)value != HashResult.Canceled)
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

    internal class VisibTotalProgressCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((QueueState)value != QueueState.Started)
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

    internal class ButtonEnableCvt : IValueConverter
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

    internal class ButtonNotEnableCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((QueueState)value == QueueState.Started)
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

    internal class ProgressWidthCvt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AlgoType algoType = (AlgoType)value;
            switch (algoType)
            {
                case AlgoType.SHA1:
                    return 270D;
                case AlgoType.SHA224:
                    return 380D;
                case AlgoType.SHA256:
                    return 430D;
                case AlgoType.SHA384:
                    return 650D;
                case AlgoType.SHA512:
                    return 860D;
                default:
                case AlgoType.MD5:
                    return 210D;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class BtnSelectFileEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/select_file_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/select_file_32_gray.png", UriKind.Relative));

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

    internal class BtnSelectFolderEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/select_folder_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/select_folder_32_gray.png", UriKind.Relative));

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

    internal class BtnClearPanelEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/clear_panel_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/clear_panel_32_gray.png", UriKind.Relative));

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

    internal class BtnCancelAllEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/cancel_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/cancel_32_gray.png", UriKind.Relative));

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

    internal class BtnContinueAllEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/continue_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/continue_32_gray.png", UriKind.Relative));

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

    internal class BtnPauseAllEnabledImgCvt : IValueConverter
    {
        private static readonly BitmapImage enabled =
            new BitmapImage(new Uri("/Images/pause_32.png", UriKind.Relative));
        private static readonly BitmapImage disabled =
            new BitmapImage(new Uri("/Images/pause_32_gray.png", UriKind.Relative));

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
}
