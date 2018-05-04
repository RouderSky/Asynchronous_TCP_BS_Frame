using System.Net;
using System.Net.Sockets;

using Common;
using Server.Logic;

namespace Server.Core {
    public class ServNet {
        public Socket listenfd;

        public int maxConn = 50;

        public static ServNet instance;

        //心跳处理
        public System.Timers.Timer timer = new System.Timers.Timer(1000);      //检测用定时器
        public long maxHeartBeatInterval = 50;        //最大通信间隔，单位是秒，有客户端超过这个时间没有通信过就断开

        //协议处理
        //由用户启动服务器的时候设置
        public ProtocolBase proto;      //服务器当前编码协议的格式，本身没有具体的消息

        //消息分发
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();

        public ServNet() {
            instance = this;
        }
    }
}
