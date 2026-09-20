using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HashCalculator
{
    public class AlgoGroupModel : BaseViewModel
    {
        private int _selectedAlgoCount = 0;

        public AlgoGroupModel(string name, AlgoInOutModel[] models) :
            this(name, models == null ? null : new ObservableCollection<AlgoInOutModel>(models))
        {
        }

        /// <summary>
        /// 以已有的集合作为本组条目集合，供"总览视图"使用：<br/>
        /// 它直接持有 AlgorithmsModel 中作为排序依据的主列表。
        /// </summary>
        public AlgoGroupModel(string name, ObservableCollection<AlgoInOutModel> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            this.Items = items;
            this.GroupName = name;
            foreach (AlgoInOutModel model in this.Items)
            {
                model.PropertyChanged += this.ItemSelectionChanged;
            }
        }

        public string GroupName { get; }

        public ObservableCollection<AlgoInOutModel> Items { get; }

        public int SelectedAlgoCount
        {
            get => this._selectedAlgoCount;
            set => this.SetPropNotify(ref this._selectedAlgoCount, value);
        }

        /// <summary>
        /// 按主列表（总览视图的条目顺序）重排本组的条目：<br/>
        /// 本组的成员不变，只把它们的顺序调整得与主列表中的相对顺序一致。
        /// </summary>
        public void SyncOrderFrom(IList<AlgoInOutModel> masterList)
        {
            int insertIndex = 0;
            foreach (AlgoInOutModel model in masterList)
            {
                int oldIndex = this.Items.IndexOf(model);
                if (oldIndex < 0)
                {
                    continue;
                }
                if (oldIndex != insertIndex)
                {
                    this.Items.Move(oldIndex, insertIndex);
                }
                insertIndex++;
            }
        }

        private void ItemSelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlgoInOutModel.Selected) && sender is AlgoInOutModel model)
            {
                this.SelectedAlgoCount += model.Selected ? 1 : -1;
            }
        }
    }
}
