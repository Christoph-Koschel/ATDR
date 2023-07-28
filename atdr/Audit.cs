using System;
using System.Linq;
using Newtonsoft.Json;

namespace ATDR
{
    public class AuditRow
    {
        public Guid record;
        public string table;

        public ChangedData changedData;
    }

    public class ChangedData
    {
        [JsonProperty]
        public ChangedItem[] changedAttributes { get; set; }

        public ChangedItem GetItem(string logicalName) {
            return changedAttributes.First(i => i.logicalName == logicalName);
        }
    }

    public class ChangedItem
    {
        [JsonProperty]
        public string logicalName;
        [JsonProperty]
        public string oldValue;
        [JsonProperty]
        public string newValue;
    }
}