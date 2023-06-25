using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
                        IEnumerable<FileInfo> fInfos;
                        switch (this.searchPolicy)
                        {
                            default:
                            case SearchPolicy.Children:
                                fInfos = dInfo.EnumerateFiles();
                                break;
                            case SearchPolicy.Descendants:
                                fInfos = dInfo.EnumerateFiles("*", SearchOption.AllDirectories);
                                break;
                            case SearchPolicy.DontSearch:
                                continue;
                        }
                        if (this.hashBasis is null)
                        {
                            foreach (FileInfo fileInfo in fInfos)
                            {
                                yield return new ModelArg(fileInfo.FullName);
                                if (this.StopSearchingToken != null &&
                                    this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                            }
                        }
                        else
                        {
                            foreach (FileInfo fileInfo in fInfos)
                            {
                                if (this.hashBasis.IsExpectedFileHash(fileInfo.Name, out byte[] hash))
                                {
                                    yield return new ModelArg(hash, fileInfo.FullName);
                                }
                                if (this.StopSearchingToken != null &&
                                    this.StopSearchingToken.IsCancellationRequested)
                                {
                                    yield break;
                                }
                            }
                            foreach (string fname in this.hashBasis.FileHashDict.Keys)
                            {
                                if (!this.hashBasis.FileHashDict[fname].IsExplored)
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
                    else
                    {
                        if (this.hashBasis.IsExpectedFileHash(Path.GetFileName(path), out byte[] hash))
                        {
                            yield return new ModelArg(hash, path);
                        }
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
