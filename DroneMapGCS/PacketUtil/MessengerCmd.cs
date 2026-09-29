using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketUtil
{
    public class MessangerCmd : Object
    {
        public MessangerCmd(int id, string cmd)
        {
            this.id = id;
            this.cmd = cmd;
        }
        public int id;
        public string cmd;
    }
}
