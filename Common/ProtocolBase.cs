using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

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

        //ProtocolStr可以实现吗？
        public abstract void AddStream(byte[] bs);
        public abstract byte[] GetStream(int start, ref int end);
        public byte[] GetStream(int start) {
            int end = 0;
            return GetStream(start, ref end);
        }

        //ProtocolStr可以实现吗？
        public abstract void AddObject<T>(T t);
        public abstract T GetObject<T>(int start, ref int end);
        public T GetObject<T>(int start) {
            int end = 0;
            return GetObject<T>(start, ref end);
        }

        public byte[] Serialize<T>(T t) {
            using (MemoryStream stream = new MemoryStream())
            {
                try {
                    // 创建序列化类
                    BinaryFormatter bf = new BinaryFormatter();
                    //序列化到stream中
                    bf.Serialize(stream, t);
                    return stream.ToArray();
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
        }
        public T Deserialize<T>(byte[] bs) {
            using (MemoryStream stream = new MemoryStream(bs)) {
                try {
                    BinaryFormatter bf = new BinaryFormatter();
                    T t = (T)bf.Deserialize(stream);
                    return t;
                }
                catch (Exception ex) {
                    Console.WriteLine("Deserialize:" + ex.Message);
                    return default(T);
                }
            }
        }
        public abstract string GetDesc();
    }
}
