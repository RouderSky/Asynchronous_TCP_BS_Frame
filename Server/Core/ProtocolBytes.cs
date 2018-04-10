using System;
using System.Collections;
using System.Linq;

namespace Server.Core {
    class ProtocolBytes : ProtocolBase{
        public byte[] bytes;

        public override ProtocolBase Decode(byte[] readBuff, int start, int length) {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.bytes = new byte[length];
            Array.Copy(readBuff, start, protocol.bytes, 0, length);
            return protocol;
        }

        public override byte[] Encode() {
            return bytes;
        }

        public override string GetName() {
            return GetString(0);
        }

        public override string GetDesc() {
            string str = "";
            if (bytes == null)      //不是用Decode获得的对象才会有这种情况出现
                return str;
            //提取每一个字节，并组装成字符串
            for (int i = 0; i < bytes.Length; ++i) {
                int b = (int)bytes[i];
                str += b.ToString() + " ";
            }
            return str;
        }

        public void AddString(string str) {
            Int32 len = str.Length;     //为何突然使用Int32？？？
            byte[] lenBytes = BitConverter.GetBytes(len);
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);  //send不是要用Default吗？

            if (bytes == null)      //不是用Decode获得的对象才会有这种情况出现
                bytes = lenBytes.Concat(strBytes).ToArray();
            else
                bytes = bytes.Concat(lenBytes).Concat(strBytes).ToArray();
        }

        //ref是的值类型按引用传递，out呢？？？？
        public string GetString(int start, ref int end) {
            if (bytes == null)      //不是用Decode获得的对象才会有这种情况出现
                return "";
            if (bytes.Length < start + sizeof(Int32))
                return "";

            Int32 strLen = BitConverter.ToInt32(bytes, start);
            string str = System.Text.Encoding.UTF8.GetString(bytes, start + sizeof(Int32), strLen);

            end = start + sizeof(Int32) + strLen;
            return str;
        }

        public string GetString(int start) {
            int end = 0;
            return GetString(start, ref end);
        }

        public void AddInt(int num) {
            byte[] numBytes = BitConverter.GetBytes(num);
            if (bytes == null)
                bytes = numBytes;
            else
                bytes = bytes.Concat(numBytes).ToArray();
        }

        public int GetInt(int start, ref int end) {
            if (bytes == null)
                return 0;
            if (bytes.Length < start + sizeof(Int32))
                return 0;

            end = start + sizeof(Int32);
            return BitConverter.ToInt32(bytes, start);
        }

        public int GetInt(int start) {
            int end = 0;
            return GetInt(start, ref end);
        }

        public void AddFloat(float num) {
            byte[] numBytes = BitConverter.GetBytes(num);
            if (bytes == null)
                bytes = numBytes;
            else
                bytes = bytes.Concat(numBytes).ToArray();
        }

        public float GetFloat(int start, ref int end) {
            if (bytes == null)
                return 0;
            if (bytes.Length < start + sizeof(float))
                return 0;

            end = start + sizeof(float);
            return BitConverter.ToSingle(bytes, start);
        }

        public float GetFloat(int start) {
            int end = 0;
            return GetFloat(start, ref end);
        }
    }
}
