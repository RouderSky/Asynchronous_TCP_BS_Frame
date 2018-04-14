using System.Text;
using System.Threading.Tasks;

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;

using Common;
using Server.Middle;
using Server.Assistant;

//使用到conn的线程：主线程、异步Socket回调函数的线程、心跳的定时器线程
//定时器回调函数也不在主线程
//因此在处理conn时一般要上锁

//加上一个获取当前连接状态的函数
//加一个updateLastTickTime
namespace Server.Core {
    public class Conn {
        private const int BUFFER_SIZE = 1024;

        public Socket socket;

        public bool isUse = false;

        public byte[] readBuff = new byte[BUFFER_SIZE];
        public int buffCount = 0;

        public byte[] lenBytes = new byte[sizeof(UInt32)];
        public Int32 msgLength = 0;

        public long lastTickTime = long.MinValue;   //该连接上一次通信时间

        public Player player;

        public Conn() {
            //readBuff = new byte[BUFFER_SIZE];       //不需要再new一次
        }

        public void Init(Socket socket) {
            this.socket = socket;
            isUse = true;
            buffCount = 0;
            lastTickTime = Sys.GetTimeStamp(); 
        }

        public int BuffRemain() {
            return BUFFER_SIZE - buffCount;
        }

        public string GetAddress() {
            if (!isUse)
                return "无法获取地址";
            return socket.RemoteEndPoint.ToString();
        }

        public void Close() {
            if (!isUse)
                return;
            if (player != null) {
                /*
                player.Logout();
                return;         //就退出了？因为Logout会再调用Close
                */
                if (!player.Logout()) {
                    Console.WriteLine("玩家数据保存失败，无法关闭连接");
                    return;
                }
            }
            Console.WriteLine("[断开连接]" + GetAddress());

            socket.Shutdown(SocketShutdown.Both);       //？？？
            socket.Close();
            isUse = false;
        }

        public void Send(ProtocolBase protocol) {
            ServNet.instance.Send(this, protocol);
        }
    }
}
