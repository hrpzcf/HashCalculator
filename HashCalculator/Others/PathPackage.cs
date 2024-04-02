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
            this(parent, paths, checklist: null, method)
        {
        }

        public PathPackage(string parent, string path, HashChecklist checklist, SearchMethod method) :
             this(parent, paths: new string[] { path }, checklist, method)
        {
        }

        public PathPackage(string parent, IEnumerable<string> paths, HashChecklist checklist, SearchMethod method)
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

        private IEnumerable<string> EnumerateFiles(string root)
        {
            switch (this.searchMethod)
            {
                default:
                case SearchMethod.Children:
                    foreach (string file in Directory.EnumerateFiles(root))
                    {
                        yield return file;
                    }
                    break;
                case SearchMethod.Descendants:
                    Stack<string> directoryExplorer = new Stack<string>();
                    directoryExplorer.Push(root);
                    while (directoryExplorer.Count > 0)
                    {
                        IEnumerable<string> fileFullPathsEnumerable;
                        string currentDirectory = directoryExplorer.Pop();
                        try
                        {
                            fileFullPathsEnumerable = Directory.EnumerateFiles(currentDirectory);
                            string[] currentSubDirectories = Directory.GetDirectories(currentDirectory);
                            Array.Reverse(currentSubDirectories);
                            foreach (string directory in currentSubDirectories)
                            {
                                directoryExplorer.Push(directory);
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        foreach (string fileFullPath in fileFullPathsEnumerable)
                        {
                            yield return fileFullPath;
                        }
                    }
                    break;
                case SearchMethod.DontSearch:
                    break;
            }
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
                        foreach (string fileFullPath in this.EnumerateFiles(path))
                        {
                            if (this.StopSearchingToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                            yield return new HashModelArg(this.parentDir, fileFullPath, this.PresetAlgoTypes,
                                this.hashChecklist);
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
                            foreach (string fileFullPath in this.EnumerateFiles(path))
                            {
                                if (this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                if (this.hashChecklist.IsNameInChecklist(Path.GetFileName(fileFullPath)))
                                {
                                    yield return new HashModelArg(this.parentDir, fileFullPath,
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
