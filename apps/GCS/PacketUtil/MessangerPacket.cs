using System;
using System.Security.Permissions;

namespace PacketUtil
{
    public class MessangerPacket : Object
    {
        public MessangerPacket(int port)
        {
            this.port = port;
         }
        public int port;
        public byte[] Packet=new byte[0];
        public long time;
    }


}
