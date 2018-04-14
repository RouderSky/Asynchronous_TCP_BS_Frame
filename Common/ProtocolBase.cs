using System.Collections.Generic;

namespace Common {

    //协议基类
    //既是工厂，也是具体对象
    public abstract class ProtocolBase {
        //工厂方法：构造出对应的协议信息，具体的协议信息不是通过一般的构造方法得到，而是通过这个函数
        public abstract ProtocolBase Decode(byte[] readBuff, int start, int length);
        public abstract byte[] Encode();

        public abstract string GetName();

        public abstract void AddString(string str);
        public abstract string GetString(int start, ref int end);
        public string GetString(int start) {
            int end = 0;
            return GetString(start, ref end);
        }

        public abstract void AddInt(int num);
        public abstract int GetInt(int start, ref int end);
        public int GetInt(int start) {
            int end = 0;
            return GetInt(start, ref end);
        }

        public abstract void AddFloat(float num);
        public abstract float GetFloat(int start, ref int end);
        public float GetFloat(int start) {
            int end = 0;
            return GetFloat(start, ref end);
        }

        public abstract string GetDesc();
    }
}
