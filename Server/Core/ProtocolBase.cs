using System.Collections.Generic;

namespace Server.Core {

    //需要将AddString、AddInt、AddFloat都作为虚函数..............................

    //协议基类
    //既是工厂，也是具体对象
    public class ProtocolBase {
        //所有函数都应该写成纯虚函数

        //工厂方法：构造出对应的协议信息，具体的协议信息不是通过一般的构造方法得到，而是通过这个函数
        public virtual ProtocolBase Decode(byte[] readBuff, int start, int length) {
            return new ProtocolBase();
        }

        public virtual byte[] Encode() {
            return new byte[] { };
        }

        public virtual string GetName() {
            return "";
        }

        public virtual string GetDesc() {
            return "";
        }
    }
}
