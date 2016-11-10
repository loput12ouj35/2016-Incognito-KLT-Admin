using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP
{
    class Attr
    {
        public enum attrType { qi, sa, attr};

        public string name { get; set; }
        public attrType type { get; set; }
        public bool numerical { get; set; }
        public string path { get; set; }


        public Attr(string name = "", attrType type = attrType.attr, bool numerical = false, string path = "")
        {
            this.name = name;
            this.type = type;
            this.numerical = numerical;
            this.path = path;
        }

        public override string ToString()
        {
            return name;
        }

    }
}
