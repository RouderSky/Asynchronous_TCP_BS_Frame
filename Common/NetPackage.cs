using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    public class NetPackage {
        public const int HEADER_SIZE = sizeof(Int32);
        const int BODY_SIZE = 1024;

        public byte[] buffer = new byte[HEADER_SIZE + BODY_SIZE];
        public int bufferCount = 0;

        public byte[] lenBytes = new byte[HEADER_SIZE];         //原本是sizeof(UInt32)
        public Int32 msgLength = 0;

        public int BuffRemain() {
            return HEADER_SIZE + BODY_SIZE - bufferCount;
        }

        static public byte[] EncoderHeader(ProtocolBase protocol) {
            byte[] bytes = protocol.Encode();     //为什么这里发送用的是UTF8？到底什么时候该用什么格式
            byte[] length = BitConverter.GetBytes((Int32)bytes.Length);
            byte[] sendBuff = length.Concat(bytes).ToArray();           //为什么还要ToArray？
            return sendBuff;
        }

        public bool DecodeHeader() {
            if (bufferCount < NetPackage.HEADER_SIZE)
                return false;
            else {
                Array.Copy(buffer, lenBytes, HEADER_SIZE);
                msgLength = BitConverter.ToInt32(lenBytes, 0);
                return true;
            }
        }

        public bool HasData() {
            if (bufferCount > 0)
                return true;
            return false;
        }

        public bool HasEnoughData() {
            if (bufferCount < msgLength + HEADER_SIZE)
                return false;
            return true;
        }

        public void DeleteCurData() {
            int count = bufferCount - msgLength - HEADER_SIZE;
            Array.Copy(buffer, HEADER_SIZE + msgLength, buffer, 0, count);
            bufferCount = count;
        }
    }
}
