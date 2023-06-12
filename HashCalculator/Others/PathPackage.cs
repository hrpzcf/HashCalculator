using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HashCalculator
{
    internal class PathPackage : IEnumerable<ModelArg>
    {
        private readonly IEnumerable<string> anyPaths;
        private readonly SearchPolicy searchPolicy;
        private readonly Dictionary<string, List<string>> hashBasis = null;

        public PathPackage(IEnumerable<string> paths, SearchPolicy policy)
        {
            this.anyPaths = paths;
            this.searchPolicy = policy;
        }

        public PathPackage(
            IEnumerable<string> paths, SearchPolicy policy, Dictionary<string, List<string>> basis)
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

        private bool ExpectedFileHash(string fname, out string hash)
        {
            foreach (KeyValuePair<string, List<string>> pair in this.hashBasis)
            {
                if (fname.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    this.hashBasis[pair.Key] = null;
                    string first = pair.Value.FirstOrDefault();
                    if (first != null && pair.Value.All(s => s == first))
                    {
                        hash = first;
                        return true;
                    }
                    hash = string.Empty;
                    return true;
                }
            }
            hash = default;
            return false;
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
                                if (this.ExpectedFileHash(fileInfo.Name, out string hash))
                                {
                                    yield return new ModelArg(hash, fileInfo.FullName);
                                }
                            }
                            foreach (string fname in this.hashBasis.Keys)
                            {
                                if (this.hashBasis[fname] != null)
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
                        if (this.ExpectedFileHash(Path.GetFileName(path), out string hash))
                        {
                            yield return new ModelArg(hash, path);
                        }
                    }
                }
            }
        }
    }
}
