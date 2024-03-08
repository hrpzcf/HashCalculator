using System.Collections.Generic;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class FileSizeFilter : AbsHashViewFilter
    {
        public override ContentControl UserInterface { get; }

        public override string Display => "文件大小";

        public override string Description => "筛选出符合指定大小范围的文件。";

        public override object Param { get; set; }

        public override object[] Items { get; set; } = new GenericItemModel[]
        {
            new GenericItemModel("B", 1),
            new GenericItemModel("KB", 1024),
            new GenericItemModel("MB", 1024 * 1024),
            new GenericItemModel("GB", 1024 * 1024 * 1024),
        };

        public double MinFileSize { get; set; }

        public double MaxFileSize { get; set; }

        public GenericItemModel MinSizeUnit { get; set; }

        public GenericItemModel MaxSizeUnit { get; set; }

        public FileSizeFilter()
        {
            this.MinSizeUnit = (GenericItemModel)this.Items[2];
            this.MaxSizeUnit = (GenericItemModel)this.Items[2];
            this.UserInterface = new FileSizeFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && this.MinSizeUnit.ItemValue is int minSizeUnit &&
                this.MaxSizeUnit.ItemValue is int maxSizeUnit)
            {
                double lowerLimit = this.MinFileSize * minSizeUnit;
                double upperLimit = this.MaxFileSize * maxSizeUnit;
                foreach (HashViewModel model in models)
                {
                    if (model.Matched)
                    {
                        if ((lowerLimit >= 0 && model.FileSize < lowerLimit) ||
                            (upperLimit >= 0 && upperLimit >= lowerLimit && model.FileSize > upperLimit))
                        {
                            model.Matched = false;
                        }
                    }
                }
            }
        }
    }
}
