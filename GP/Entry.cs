using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP
{
    public class Entry      //<map> 하위의 <entry>노드
    {
        public readonly string key;      //name으로 저장됨
        public int count;
        public int index;       //value로 저장됨
        public readonly bool hideIndex;

        public Entry(string key, int count, int index = 0, bool hideIndex = false)
        {
            this.key = key;
            this.count = count;
            this.index = index;
            this.hideIndex = hideIndex;
        }

        public override string ToString()
        {
            string tmp = hideIndex ? "" : index + "번:\t";
            return tmp + key + " (#" + count + "개)";
        }
    }
}
