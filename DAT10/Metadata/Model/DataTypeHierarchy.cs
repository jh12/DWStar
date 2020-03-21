using System.Collections.Generic;
using System.Data.OleDb;

namespace DAT10.Metadata.Model
{
    /// <summary>
    /// Datatype hierarchy
    /// </summary>
    public class DataTypeHierarchy
    {
        public HierarchyNode Root;

        public DataTypeHierarchy(HierarchyNode root)
        {
            Root = root;
        }
    }

    /// <summary>
    /// Node of a datatype hierarchy
    /// </summary>
    public struct HierarchyNode
    {
        // Datatype
        public OleDbType OlebDbType;

        // Children of datatype
        public List<HierarchyNode> Children;

        public HierarchyNode(OleDbType olebDbType)
        {
            OlebDbType = olebDbType;
            Children = new List<HierarchyNode>();
        }
    }
}