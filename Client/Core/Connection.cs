using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Common;
using Assistant;

namespace Client.Core {
    public class Connection {
        public Socket socket;

        const int BUFFER_SIZE = 1024;
        byte[] readBuff = new byte[BUFFER_SIZE];
        int buffCount = 0;

        Int32 msgLength = 0;
        byte[] lenBytes = new byte[sizeof(Int32)];

        public ProtocolBase proto;

        //改用时间戳，未测试.................
        long lastTickTime = Sys.GetTimeStamp();
        long heartBeatTime = 30;
        ////////////////////////

        public MsgDistribution msgDist = new MsgDistribution();

        enum Status {
            None,
            Connected,
        };

        Status status = Status.None;

        public bool Connect(string host, int port) {
            if (status == Status.Connected) {
                Console.WriteLine("请先断开之前的连接再重新连接");
                return false;
            }

            try {
                socket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream,
                                    ProtocolType.Tcp);

                socket.Connect(host, port);

                socket.BeginReceive(readBuff,
                                    buffCount,
                                    BUFFER_SIZE - buffCount,
                                    SocketFlags.None,
                                    ReceiveCb,
                                    null);      //改

                Console.WriteLine("连接成功");

                status = Status.Connected;
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("连接失败：" + e.Message);
                return false;
            }
        }

        public bool Close() {
            if (status != Status.Connected) {
                Console.WriteLine("尚未连接，无需断开连接");
                return false;
            }

            try {
                socket.Close();
                status = Status.None;       //改
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("关闭连接失败：" + e.Message);
                return false;
            }
        }

        public void ReceiveCb(IAsyncResult ar) {
            try {
                int count = socket.EndReceive(ar);

                if (count <= 0) {
                    Console.WriteLine("收到服务器断开连接");
                    Close();
                    return;
                }

                buffCount = buffCount + count;
                ProcessData();
                socket.BeginReceive(readBuff,
                                    buffCount,
                                    BUFFER_SIZE - buffCount,
                                    SocketFlags.None,
                                    ReceiveCb,
                                    null);      //改
            }
            catch (Exception e) {
                Console.WriteLine("ReceiveCb失败：" + e.Message);
                status = Status.None;
            }
        }

        //处理粘包、分包
        public void ProcessData() {
            if (buffCount < sizeof(Int32))
                return;
            Array.Copy(readBuff, lenBytes, sizeof(Int32));
            msgLength = BitConverter.ToInt32(lenBytes, 0);
            if (msgLength > buffCount - sizeof(Int32))
                return;

            ProtocolBase protocol = proto.Decode(readBuff, sizeof(Int32), msgLength);
            Console.WriteLine("收到消息 " + protocol.GetDesc());
            
            msgDist.AddMsg(protocol);

            int count = buffCount - msgLength - sizeof(Int32);
            Array.Copy(readBuff, sizeof(Int32) + msgLength, readBuff, 0, count);
            buffCount = count;
            if (buffCount > 0) {
                ProcessData();
            }
        }

        public bool Send(ProtocolBase protocol) {
            if (status != Status.Connected) {
                Console.WriteLine("尚未连接，发送失败");
                return false;
            }

            byte[] content = protocol.Encode();
            byte[] length = BitConverter.GetBytes(content.Length);
            byte[] sendBuff = length.Concat(content).ToArray();
            socket.Send(sendBuff);
            //Debug.Log("发送消息 " + protocol.GetDesc());
            return true;
        }

        bool Send(ProtocolBase protocol, string cbName, MsgDistribution.Delegate cb) {
            bool ret = Send(protocol);
            if (ret)
                msgDist.AddOnceListener(cbName, cb);
            return ret;
        }

        public bool Send(ProtocolBase protocol, MsgDistribution.Delegate cb) {
            string cbName = protocol.GetName();
            return Send(protocol, cbName, cb);
        }

        public void Update() {
            msgDist.Update();

            if (status == Status.Connected) {
                if (Sys.GetTimeStamp() - lastTickTime > heartBeatTime) {
                    ProtocolBase protocol = proto.Decode(null, 0, 0);
                    protocol.AddString("HeartBeat");
                    Send(protocol);
                    lastTickTime = Sys.GetTimeStamp();
                }
            }
        }
    }
}
