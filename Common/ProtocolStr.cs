using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//未测试.....................

namespace Common {
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

        public override void AddString(string str) {
            this.str += "," + str;
        }
        public override string GetString(int start, ref int end) {
            string[] args = str.Split(',');
            end = start + 1;
            return args[start];
        }

        public override void AddInt(int num) {
            this.str += "," + num.ToString();
        }
        public override int GetInt(int start, ref int end) {
            string[] args = str.Split(',');
            end = start + 1;
            return int.Parse(args[start]);
        }

        public override void AddFloat(float num) {
            this.str += "," + num.ToString();
        }
        public override float GetFloat(int start, ref int end) {
            string[] args = str.Split(',');
            end = start + 1;
            return float.Parse(args[start]);
        }

        public override string GetDesc() {
            return str;
        }
    }
}
