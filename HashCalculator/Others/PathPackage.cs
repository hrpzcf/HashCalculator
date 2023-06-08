using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HashCalculator
{
    internal class PathPackage
    {
        private readonly IEnumerable<string> paths;
        private readonly SearchPolicy policy;
        private readonly Dictionary<string, List<string>> basis = null;
        private readonly CancellationToken token;

        public PathPackage(
            IEnumerable<string> paths,
            SearchPolicy policy,
            CancellationToken token)
        {
            this.paths = paths;
            this.policy = policy;
            this.token = token;
        }

        public PathPackage(
            IEnumerable<string> paths,
            SearchPolicy policy,
            Dictionary<string, List<string>> basis,
            CancellationToken token)
        {
            this.paths = paths;
            this.policy = policy;
            this.basis = basis;
            this.token = token;
        }

        private bool ExpectedFileHash(string fname, out string hash)
        {
            foreach (KeyValuePair<string, List<string>> pair in this.basis)
            {
                if (fname.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    this.basis[pair.Key] = null;
                    Console.WriteLine($"pair.Value: {pair.Value}");
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

        public IEnumerable<ModelArg> EnumerateModelArgs()
        {
            if (this.paths is null)
            {
                yield break;
            }
            foreach (string path in this.paths)
            {
                if (this.token.IsCancellationRequested)
                {
                    yield break;
                }
                if (File.Exists(path))
                {
                    if (this.basis is null)
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
                else if (Directory.Exists(path))
                {
                    try
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(path);
                        IEnumerable<FileInfo> fInfos;
                        switch (this.policy)
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
                        if (this.basis is null)
                        {
                            foreach (FileInfo fileInfo in fInfos)
                            {
                                if (this.token.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                yield return new ModelArg(fileInfo.FullName);
                            }
                        }
                        else
                        {
                            foreach (FileInfo fileInfo in fInfos)
                            {
                                if (this.token.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                if (this.ExpectedFileHash(fileInfo.Name, out string hash))
                                {
                                    yield return new ModelArg(hash, fileInfo.FullName);
                                }
                            }
                            foreach (string fname in this.basis.Keys)
                            {
                                if (this.token.IsCancellationRequested)
                                {
                                    yield break;
                                }
                                if (this.basis[fname] != null)
                                {
                                    yield return new ModelArg(fname, true);
                                }
                            }
                        }
                    }
                    finally { }
                }
            }
        }
    }
}
