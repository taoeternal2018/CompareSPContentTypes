using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareSPContentTypes
{
    public class ContentType
    {
        public ContentType()
        {
            Fields = new List<ContentTypeField>();
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public List<ContentTypeField> Fields { get; set; }
    }
}
