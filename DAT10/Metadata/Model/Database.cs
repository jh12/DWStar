using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Metadata.Model
{
    public class Database
    {
        #region Properties

        /// <summary>
        /// Name of database
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// List of tables
        /// </summary>
        public List<Table> Tables { get; set; }

        public string OriginType { get; set; }

        #endregion

        public Database(string name, string connectionString)
        {
            Name = name;
            Tables = new List<Table>();
            ConnectionString = connectionString;
        }

        public Database(string name, string connectionString, string originType)
        {
            Name = name;
            Tables = new List<Table>();
            ConnectionString = connectionString;
            OriginType = originType;
        }



        protected bool Equals(Database other)
        {
            return string.Equals(Name, other.Name) && string.Equals(ConnectionString, other.ConnectionString);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Database)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
            }
        }
    }
}
