using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HashCalculator
{
    internal class PathPackage : IEnumerable<ModelArg>
    {
        private static readonly char[] invalidFnameChars = Path.GetInvalidFileNameChars();

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy)
        {
            this.paths = paths;
            this.searchPolicy = policy;
        }

        public PathPackage(string path, SearchPolicy policy, HashChecklist checklist)
        {
            this.paths = new string[] { path };
            this.searchPolicy = policy;
            this.hashChecklist = checklist;
        }

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy, HashChecklist checklist)
        {
            this.paths = paths;
            this.searchPolicy = policy;
            this.hashChecklist = checklist;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.EnumerateModelArgs();
        }

        public IEnumerator<ModelArg> GetEnumerator()
        {
            return this.EnumerateModelArgs();
        }

        private IEnumerator<ModelArg> EnumerateModelArgs()
        {
            foreach (string path in this.paths ?? Array.Empty<string>())
            {
                if (this.StopSearchingToken != null &&
                    this.StopSearchingToken.IsCancellationRequested)
                {
                    yield break;
                }
                if (Directory.Exists(path))
                {
                    try
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(path);
                        IEnumerator<FileInfo> fileInfoEnum;
                        switch (this.searchPolicy)
                        {
                            default:
                            case SearchPolicy.Children:
                                fileInfoEnum = dInfo.EnumerateFiles().GetEnumerator();
                                break;
                            case SearchPolicy.Descendants:
                                fileInfoEnum = dInfo.EnumerateFiles("*", SearchOption.AllDirectories).GetEnumerator();
                                break;
                            case SearchPolicy.DontSearch:
                                continue;
                        }
                        // 不直接 foreach 遍历 EnumerateFiles 返回的 IEnumerable 的原因：
                        // 遍历过程中捕捉不到 UnauthorizedAccessException 异常(?)
                        while (true)
                        {
                            try
                            {
                                if (!fileInfoEnum.MoveNext())
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                // 不使用 continue 而使用 break 的原因：如果因权限等问题导致 MoveNext 抛出异常，
                                // 那么下次 MoveNext 结果肯定是 false(?)，使用 continue 没有意义
                                break;
                            }
                            if (this.hashChecklist == null)
                            {
                                yield return new ModelArg(fileInfoEnum.Current.FullName, this.PresetAlgoType);
                            }
                            else if (this.hashChecklist.IsNameInChecklist(fileInfoEnum.Current.Name))
                            {
                                yield return new ModelArg(this.hashChecklist, fileInfoEnum.Current.FullName, this.PresetAlgoType);
                            }
                            if (this.StopSearchingToken != null && this.StopSearchingToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                        }
                        if (this.hashChecklist != null)
                        {
                            foreach (KeyValuePair<string, AlgHashMap> pair in this.hashChecklist)
                            {
                                if (pair.Key.IndexOfAny(invalidFnameChars) != -1)
                                {
                                    yield return new ModelArg(true, true, this.PresetAlgoType);
                                }
                                // 此属性由 HashChecklist.IsNameInChecklist 方法更改
                                else if (!pair.Value.IsExistingFile)
                                {
                                    yield return new ModelArg(pair.Key, true, this.PresetAlgoType);
                                }
                                if (this.StopSearchingToken != null && this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                            }
                        }
                    }
                    finally { }
                }
                else if (File.Exists(path))
                {
                    yield return new ModelArg(this.hashChecklist, path, this.PresetAlgoType);
                }
            }
        }

        private readonly IEnumerable<string> paths;
        private readonly SearchPolicy searchPolicy;
        private readonly HashChecklist hashChecklist = null;

        public CancellationToken StopSearchingToken { get; set; }

        public AlgoType PresetAlgoType { get; set; } = AlgoType.Unknown;
    }
}
