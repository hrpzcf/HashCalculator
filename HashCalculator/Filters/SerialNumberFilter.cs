using System.Collections.Generic;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class SerialNumberFilter : AbsHashViewFilter
    {
        public override ContentControl UserInterface { get; }

        public override string Display => "序号范围";

        public override string Description => "将序列号在指定范围内的行筛选出来。";

        public override object Param { get; set; }

        public override object[] Items { get; set; }

        public int SerialLeft { get; set; }

        public int SerialRight { get; set; }

        public SerialNumberFilter()
        {
            this.UserInterface = new SerialNumberFilterCtrl(this);
        }

        public override void FilterObjects(IEnumerable<HashViewModel> models)
        {
            if (models != null)
            {
                foreach (HashViewModel model in models)
                {
                    if ((this.SerialLeft >= 0 && model.SerialNumber < this.SerialLeft) ||
                        (this.SerialRight >= 0 && this.SerialRight >= this.SerialLeft && model.SerialNumber > this.SerialRight))
                    {
                        model.Matched = false;
                    }
                }
            }
        }
    }
}
