using System;
using System.Threading.Tasks;

namespace HashCalculator
{
    internal static class ModelEventExt
    {
        /// <summary>
        /// 为 HashViewModel 的 ModelCapturedEvent 和 ModelReleasedEvent 事件提供异步执行方法 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="model"></param>
        public static async void InvokeAsync(this Action<HashViewModel> action, HashViewModel model)
        {
            await Task.Run(() => { action(model); });
        }
    }
}
