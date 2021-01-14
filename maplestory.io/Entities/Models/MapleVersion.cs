using Newtonsoft.Json;
using PKG1;
using System.Collections.Generic;

namespace maplestory.io.Entities.Models
{
    public class MapleVersion
    {
        public long Id { get; set; }
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public Region Region { get; set; }
        public string MapleVersionId { get; set; }
        public string BaseFile { get; set; }
        public string Location { get; set; }
        public long? BasedOffOf { get; set; }
        public bool IsReady { get; set; }

        public List<VersionPathHash> VersionPathHashes { get; set; }
    }
}
