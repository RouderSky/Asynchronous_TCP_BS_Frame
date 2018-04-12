using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Server.Core {
    public class ProtocolStr : ProtocolBase {
        public string str;

        public override ProtocolBase Decode(byte[] readBuff, int start, int length) {
            ProtocolStr protocol = new ProtocolStr();
            if(readBuff != null)
                protocol.str = System.Text.Encoding.UTF8.GetString(readBuff, start, length);
            return (ProtocolBase)protocol;
        }

        public override byte[] Encode() {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(str);
            return b;
        }

        public override string GetName() {
            if (str.Length == 0)
                return "";
            return str.Split(',')[0];
        }

        public override string GetDesc() {
            return str;
        }
    }
}
