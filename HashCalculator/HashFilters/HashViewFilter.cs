﻿using System.Collections.Generic;
using System.ComponentModel;

namespace HashCalculator
{
    internal abstract class HashViewFilter
    {
        public abstract string Display { get; }

        public abstract string Description { get; }

        public abstract object Param { get; set; }

        public abstract object[] Items { get; set; }

        public virtual SortDescription[] SortDescriptions { get; }

        public virtual GroupDescription[] GroupDescriptions { get; }

        public virtual ControlItem[] FilterLogics { get; set; } = new ControlItem[]
        {
            new ControlItem("满足任意要求", FilterLogic.Any),
            new ControlItem("严格满足要求", FilterLogic.Strict),
            new ControlItem("在要求范围内", FilterLogic.Within),
            new ControlItem("涵盖所有要求", FilterLogic.Cover),
        };

        public bool Selected { get; set; }

        public virtual void Init() { }

        public virtual void Reset() { }

        public abstract void FilterObjects(IEnumerable<HashViewModel> models);
    }
}
