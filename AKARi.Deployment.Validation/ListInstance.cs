using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareSPContentTypes
{
    public class ListInstance
    {
        public ListInstance()
        {
            ListFields = new List<ListField>();
            ContentTypeBindings = new List<string>();
        }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string DocumentTemplate { get; set; }
        public string TemplateType { get; set; }
        public List<ListField> ListFields { get; set; }
        public List<string> ContentTypeBindings { get; set; }
    }
}
