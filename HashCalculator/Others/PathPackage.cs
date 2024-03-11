using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HashCalculator
{
    internal class PathPackage : IEnumerable<HashModelArg>
    {
        private readonly string[] paths = null;
        private readonly SearchMethod searchMethod;
        private readonly HashChecklist hashChecklist = null;
        private readonly string pathsParentDir = null;
        private static readonly char[] invalidFnameChars = Path.GetInvalidFileNameChars();

        public CancellationToken StopSearchingToken { get; set; }

        public IEnumerable<AlgoType> PresetAlgoTypes { get; set; }

        public PathPackage(string parent, IEnumerable<string> paths, SearchMethod method) :
            this(parent, paths, method, checklist: null)
        {
        }

        public PathPackage(string parent, string path, SearchMethod method, HashChecklist checklist) :
             this(parent, paths: new string[] { path }, method, checklist)
        {
        }

        public PathPackage(string parent, IEnumerable<string> paths, SearchMethod method, HashChecklist checklist)
        {
            this.pathsParentDir = parent;
            this.paths = paths is string[] array ? array : paths.ToArray();
            this.searchMethod = method;
            this.hashChecklist = checklist;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.EnumerateModelArgs();
        }

        public IEnumerator<HashModelArg> GetEnumerator()
        {
            return this.EnumerateModelArgs();
        }

        private IEnumerator<HashModelArg> EnumerateModelArgs()
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
                    IEnumerator<FileInfo> enumerator;
                    try
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        switch (this.searchMethod)
                        {
                            default:
                            case SearchMethod.Children:
                                enumerator = directoryInfo.EnumerateFiles().GetEnumerator();
                                break;
                            case SearchMethod.Descendants:
                                enumerator = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                    .GetEnumerator();
                                break;
                            case SearchMethod.DontSearch:
                                continue;
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    // 不直接遍历 EnumerateFiles 返回的 IEnumerable 的原因：
                    // 遍历过程中捕捉不到 UnauthorizedAccessException 异常(?)
                    while (true)
                    {
                        try
                        {
                            if (!enumerator.MoveNext())
                            {
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // 不使用 continue 而使用 break 的原因：
                            // 如果因权限等问题导致 MoveNext 抛出异常，那下一轮结果也是 false(?)
                            break;
                        }
                        if (this.hashChecklist == null ||
                            this.hashChecklist.IsNameInChecklist(enumerator.Current.Name))
                        {
                            yield return new HashModelArg(this.pathsParentDir,
                                enumerator.Current.FullName, this.PresetAlgoTypes, this.hashChecklist);
                        }
                        if (this.StopSearchingToken.IsCancellationRequested)
                        {
                            yield break;
                        }
                    }
                    if (this.hashChecklist != null)
                    {
                        foreach (KeyValuePair<string, AlgHashMap> pair in this.hashChecklist)
                        {
                            if (string.IsNullOrEmpty(pair.Key) || pair.Key.IndexOfAny(invalidFnameChars) != -1)
                            {
                                yield return new HashModelArg(true, true, this.PresetAlgoTypes);
                            }
                            // 此属性由 HashChecklist.IsNameInChecklist 方法更改
                            else if (!pair.Value.IsExistingFile)
                            {
                                yield return new HashModelArg(pair.Key, true, this.PresetAlgoTypes);
                            }
                            if (this.StopSearchingToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    yield return new HashModelArg(this.pathsParentDir, path, this.PresetAlgoTypes,
                        this.hashChecklist);
                }
            }
        }
    }
}
