using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class SerialNumberFilter : AbsHashViewFilter
    {
        public override ContentControl Settings { get; }

        public override string Display => "序号范围";

        public override string Description => "将指定序列号的行筛选出来";

        public override object Param { get; set; }

        public override object[] Items { get; set; }

        public int SerialLeft { get; set; }

        public int SerialRight { get; set; }

        public SerialNumberFilter()
        {
            this.Settings = new SerialNumberFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null && models.Any())
            {
                if (this.SerialLeft > this.SerialRight)
                {
                    int temp;
                    temp = this.SerialLeft;
                    this.SerialLeft = this.SerialRight;
                    this.SerialRight = temp;
                }
                foreach (HashViewModel model in models)
                {
                    if (model.SerialNumber < this.SerialLeft || model.SerialNumber > this.SerialRight)
                    {
                        model.Matched = false;
                    }
                }
            }
        }
    }
}
