using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace HashCalculator
{
    internal class PathPackage : IEnumerable<ModelArg>
    {
        public PathPackage(IEnumerable<string> paths, SearchPolicy policy)
        {
            this.paths = paths;
            this.searchPolicy = policy;
        }

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy, Basis basis)
        {
            this.paths = paths;
            this.searchPolicy = policy;
            this.hashBasis = basis;
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
                        IEnumerator<FileInfo> fiEnum;
                        switch (this.searchPolicy)
                        {
                            default:
                            case SearchPolicy.Children:
                                fiEnum = dInfo.EnumerateFiles().GetEnumerator();
                                break;
                            case SearchPolicy.Descendants:
                                fiEnum = dInfo.EnumerateFiles("*", SearchOption.AllDirectories).GetEnumerator();
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
                                if (!fiEnum.MoveNext())
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                // 不使用 continue 而使用 break 的原因：
                                // 如果因权限等问题导致 MoveNext 抛出异常，
                                // 那么下次 MoveNext 结果肯定是 false(?)，使用 continue 没有意义
                                break;
                            }
                            if (this.hashBasis is null)
                            {
                                yield return new ModelArg(fiEnum.Current.FullName);
                            }
                            else if (this.hashBasis.IsExpectedFileHash(fiEnum.Current.Name, out byte[] hash))
                            {
                                yield return new ModelArg(hash, fiEnum.Current.FullName);
                            }
                            if (this.StopSearchingToken != null &&
                                this.StopSearchingToken.IsCancellationRequested)
                            {
                                yield break;
                            }
                        }
                        if (this.hashBasis != null)
                        {
                            foreach (string fname in this.hashBasis.FileHashDict.Keys)
                            {
                                if (!this.hashBasis.FileHashDict[fname].HasBeenChecked)
                                {
                                    yield return new ModelArg(fname, true);
                                }
                            }
                        }
                    }
                    finally { }
                }
                else if (File.Exists(path))
                {
                    if (this.hashBasis is null)
                    {
                        yield return new ModelArg(path);
                    }
                    else if (this.hashBasis.IsExpectedFileHash(Path.GetFileName(path), out byte[] hash))
                    {
                        yield return new ModelArg(hash, path);
                    }
                }
            }
        }

        private readonly Basis hashBasis = null;
        private readonly IEnumerable<string> paths;
        private readonly SearchPolicy searchPolicy;

        public CancellationToken StopSearchingToken { get; set; }
    }
}
