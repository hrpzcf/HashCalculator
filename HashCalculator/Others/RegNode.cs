using Microsoft.Win32;
using System;

namespace HashCalculator
{
    internal class RegNode
    {
        public RegNode(string name)
        {
            this.Name = name ??
                throw new ArgumentNullException($"Argument can not be null: {nameof(name)}");
        }

        public string Name { get; }

        public RegNode[] Nodes { get; set; }

        public RegValue[] Values { get; set; }

        public static bool DeleteRegNode(RegistryKey root, RegNode regNode)
        {
            if (root == null)
            {
                return false;
            }
            using (root)
            {
                try
                {
                    if (regNode.Name == string.Empty)
                    {
                        return false;
                    }
                    root.DeleteSubKeyTree(regNode.Name, false);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static bool WriteRegNode(RegistryKey root, RegNode regNode)
        {
            if (root == null)
            {
                return false;
            }
            try
            {
                using (RegistryKey parent = root.CreateSubKey(regNode.Name, true))
                {
                    if (parent == null)
                    {
                        return false;
                    }
                    if (regNode.Nodes != null)
                    {
                        foreach (RegNode nextNode in regNode.Nodes)
                        {
                            if (!WriteRegNode(parent, nextNode))
                            {
                                return false;
                            }
                        }
                    }
                    if (regNode.Values != null)
                    {
                        foreach (RegValue nextValue in regNode.Values)
                        {
                            if (nextValue.Data != null)
                            {
                                parent.SetValue(nextValue.Name, nextValue.Data, nextValue.Kind);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
