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
        private readonly string parentDir = null;
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
            this.parentDir = parent;
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
            if (this.hashChecklist == null)
            {
                foreach (string path in this.paths ?? Array.Empty<string>())
                {
                    if (this.StopSearchingToken != null &&
                        this.StopSearchingToken.IsCancellationRequested)
                    {
                        yield break;
                    }
                    if (File.Exists(path))
                    {
                        yield return new HashModelArg(this.parentDir, path, this.PresetAlgoTypes,
                            this.hashChecklist);
                    }
                    else if (Directory.Exists(path))
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
                        // 不直接遍历 EnumerateFiles 方法返回的 IEnumerable 的原因：遍历过程抛出异常，
                        // 需要使用 try-catch 包裹整个 foreach，但 try-catch 内又不允许 yield return
                        while (true)
                        {
                            if (this.StopSearchingToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                            try
                            {
                                if (!enumerator.MoveNext())
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                            yield return new HashModelArg(this.parentDir, enumerator.Current.FullName,
                                this.PresetAlgoTypes, this.hashChecklist);
                        }
                    }
                }
            }
            else
            {
                if (this.hashChecklist.AssertRelativeGetFull(this.parentDir,
                    out IEnumerable<string> fullPaths))
                {
                    foreach (string path in fullPaths)
                    {
                        if (this.StopSearchingToken.IsCancellationRequested)
                        {
                            yield break;
                        }
                        HashModelArg argument = new HashModelArg(this.parentDir, path,
                            this.PresetAlgoTypes, this.hashChecklist);
                        if (!File.Exists(path))
                        {
                            argument.Deprecated = true;
                            argument.Message = "找不到文件，可能哈希值清单与此文件相对位置不正确";
                        }
                        yield return argument;
                    }
                }
                else
                {
                    foreach (string path in this.paths ?? Array.Empty<string>())
                    {
                        if (this.StopSearchingToken != null &&
                            this.StopSearchingToken.IsCancellationRequested)
                        {
                            yield break;
                        }
                        if (File.Exists(path))
                        {
                            yield return new HashModelArg(this.parentDir, path, this.PresetAlgoTypes,
                                this.hashChecklist);
                        }
                        else if (Directory.Exists(path))
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
                            // 不直接遍历 EnumerateFiles 方法返回的 IEnumerable 的原因：遍历过程抛出异常，
                            // 需要使用 try-catch 包裹整个 foreach，但 try-catch 内又不允许 yield return
                            while (true)
                            {
                                if (this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                try
                                {
                                    if (!enumerator.MoveNext())
                                    {
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                                if (this.hashChecklist.IsNameInChecklist(enumerator.Current.Name))
                                {
                                    yield return new HashModelArg(this.parentDir, enumerator.Current.FullName,
                                        this.PresetAlgoTypes, this.hashChecklist);
                                }
                            }
                            foreach (KeyValuePair<string, HashChecker> pair in this.hashChecklist)
                            {
                                if (this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                if (string.IsNullOrEmpty(pair.Key) || pair.Key.IndexOfAny(invalidFnameChars) != -1)
                                {
                                    yield return new HashModelArg(this.PresetAlgoTypes);
                                }
                                // 此属性由 HashChecklist.IsNameInChecklist 方法更改
                                else if (!pair.Value.IsExistingFile)
                                {
                                    yield return new HashModelArg(pair.Key, this.PresetAlgoTypes);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
