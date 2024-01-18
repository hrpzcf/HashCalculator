using System.Collections.Generic;
using System.Windows.Controls;

namespace HashCalculator
{
    internal class RestoreFilesCmder : AbsHashesCmder
    {
        public override ContentControl UserInterface { get; }

        public override string Display => "从有哈希标记的文件还原";

        public override string Description => "从使用本程序生成的带哈希标记的文件中还原出不带标记的原文件。";

        public RestoreFilesCmder() : this(MainWndViewModel.HashViewModels)
        {
        }

        public RestoreFilesCmder(IEnumerable<HashViewModel> models) : base(models)
        {
            this.UserInterface = new RestoreFilesCmderCtrl(this);
        }
    }
}
