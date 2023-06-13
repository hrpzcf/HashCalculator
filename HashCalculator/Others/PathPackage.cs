using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashCalculator
{
    internal class PathPackage : IEnumerable<ModelArg>
    {
        private readonly Basis hashBasis = null;
        private readonly IEnumerable<string> anyPaths;
        private readonly SearchPolicy searchPolicy;

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy)
        {
            this.anyPaths = paths;
            this.searchPolicy = policy;
        }

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy, Basis basis)
        {
            this.anyPaths = paths;
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
            if (this.anyPaths is null)
            {
                yield break;
            }
            foreach (string path in this.anyPaths)
            {
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
                            }
                            foreach (string fname in this.hashBasis.NameHashMap.Keys)
                            {
                                if (!this.hashBasis.NameHashMap[fname].IsExplored)
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
    }
}
