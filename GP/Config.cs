using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP
{
    class Config
    {
        public readonly int k;
        public readonly int l;
        public readonly float t;

        public readonly string input;
        public readonly string output;
        public readonly string log;

        public Config(int k, int l, float t, string input, string output, string log)
        {
            this.k = k;
            this.l = l;
            this.t = t;

            this.input = input;
            this.output = output;
            this.log = log;
        }

    }
}
