using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUwpBaseLib.Logs
{
    public class SqliteLogEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string LogLevel { get; set; }

        public DateTime Time { get; set; }

        public string Log { get; set; }
    }
}
