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
using Assistant;
using Server.Middle;
using Server.Logic;

//使用到conn的线程：主线程、异步Socket回调函数的线程、心跳的定时器线程
//定时器回调函数也不在主线程
//因此在处理conn时一般要上锁

//加上一个获取当前连接状态的函数
//加一个updateLastTickTime
namespace Server.Core {
    public class Conn {
        public Socket socket;

        public NetPackage recvNetPackage;

        public long lastTickTime = long.MinValue;   //上一次心跳时间

        public Player player;
        public enum Status {
            None,
            Connected,
            Login
        };

        public Status status = Status.None;

        public string GetAddress() {
            if (status == Status.None)
                return "无法获取地址";
            return socket.RemoteEndPoint.ToString();
        }
    }
}
