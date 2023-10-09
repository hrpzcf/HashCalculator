﻿using System.Collections.Generic;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class FileSizeFilter : AbsHashViewFilter
    {
        public override ContentControl Settings { get; }

        public override string Display => "文件大小";

        public override string Description => "筛选出符合指定大小范围的文件";

        public override object Param { get; set; }

        public override object[] Items { get; set; } = new ControlItem[]
        {
            new ControlItem("B", 1),
            new ControlItem("KB", 1024),
            new ControlItem("MB", 1024*1024),
            new ControlItem("GB", 1024*1024*1024),
        };

        public double MinFileSize { get; set; }

        public double MaxFileSize { get; set; }

        public ControlItem MinSizeUnit { get; set; }

        public ControlItem MaxSizeUnit { get; set; }

        public FileSizeFilter()
        {
            this.MinSizeUnit = (ControlItem)this.Items[2];
            this.MaxSizeUnit = (ControlItem)this.Items[2];
            this.Settings = new FileSizeFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models == null)
            {
                return;
            }
            if (this.MinSizeUnit.ItemValue is int minSizeUnit && this.MaxSizeUnit.ItemValue is int maxSizeUnit)
            {
                double minFileSize = this.MinFileSize * minSizeUnit;
                double maxFileSize = this.MaxFileSize * maxSizeUnit;
                if (minFileSize > maxFileSize)
                {
                    CommonUtils.Swap(ref minFileSize, ref maxFileSize);
                }
                foreach (HashViewModel model in models)
                {
                    if (model.Matched)
                    {
                        if (model.FileSize < minFileSize || model.FileSize > maxFileSize)
                        {
                            model.Matched = false;
                        }
                    }
                }
            }
        }
    }
}
