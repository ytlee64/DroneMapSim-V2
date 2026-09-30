using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public static class Tokenize
    {
        public static List<string> GetWords(string line)
        {
            List<string> tokens = new List<string>();
            char[] delim = { ' ', '\t', '=' };


            foreach (string w in line.Split(delim))
            {
                if (w.Length != 0)
                    tokens.Add(w);
            }

            return tokens;
        }
    }
}
