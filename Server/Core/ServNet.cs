using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Reflection;

using System.Text;
using System.Threading.Tasks;

using Common;
using Server.Assistant;
using Server.Logic;

namespace Server.Core {

    //要求单例.......................
    class ServNet {
        public Socket listenfd;

        public int maxConn = 50;        
        public Conn[] conns;

        public static ServNet instance;

        //心跳处理
        System.Timers.Timer timer = new System.Timers.Timer(1000);      //检测用定时器
        public long heartBeatTime = 50;        //最大通信间隔，单位是秒，有客户端超过这个时间没有通信过就断开

        //协议处理
        //由用户启动服务器的时候设置
        public ProtocolBase proto;      //只是为了用来解码构造出一个协议信息，本身没有具体的消息

        //消息分发
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();

        public ServNet() {
            instance = this;
        }

        public void HeartBeat() {
            //Console.WriteLine("[主定时器执行]");
            long timeNow = Sys.GetTimeStamp();

            for (int i = 0; i < conns.Length; ++i) {
                Conn conn = conns[i];
                //if (conn == null)       //不可能
                    //continue;
                if (conn.status == Conn.Status.None)
                    continue;

                if (timeNow - conn.lastTickTime > heartBeatTime) {
                    Console.WriteLine("[心跳引起断开连接]" + conn.GetAddress());
                    lock (conn)
                        conn.Close();
                }
            }
        }
        public void HandleMainTimer(object sender, System.Timers.ElapsedEventArgs e) {
            HeartBeat();
            timer.Start();      //这么做是确保上一次检测已经完成再开始下一次？？？
        }
        //启动服务器
        public void Start(string host, int port) {
            //心跳处理用定时器，暂时关闭
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;       //这样可以启动定时器？和Start的功能完全一样？？？

            conns = new Conn[maxConn];
            for (int i = 0; i < maxConn; ++i) {
                conns[i] = new Conn();
            }

            //Socket
            listenfd = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream,
                                  ProtocolType.Tcp);

            //Bind
            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
            listenfd.Bind(ipEp);

            //Listen
            listenfd.Listen(maxConn);

            listenfd.BeginAccept(AcceptCb, null);

            Console.WriteLine("[服务器]启动成功");
        }

        private void AcceptCb(IAsyncResult ar) {
            try {
                Socket socket = listenfd.EndAccept(ar);

                for (int i = 0; i < conns.Length; ++i) {
                    lock (conns[i]) {
                        if (conns[i].status == Conn.Status.None) {
                            Conn conn = conns[i];
                            conn.Init(socket);
                            //其实在这里就可以释放临界区了
                            string adr = conn.GetAddress();
                            Console.WriteLine("客户端连接 [" + adr + "] conn池ID：" + i);

                            conn.socket.BeginReceive(conn.readBuff,
                                                     conn.buffCount,
                                                     conn.BuffRemain(),
                                                     SocketFlags.None,
                                                     ReceiveCb,
                                                     conn);

                            //继续接收客户端连接
                            listenfd.BeginAccept(AcceptCb, null);

                            return;
                        }
                    }
                }

            }
            catch (Exception e) {
                Console.WriteLine("AcceptCb失败：" + e.Message);
            }
        }

        private void ReceiveCb(IAsyncResult ar) {
            Conn conn = (Conn)ar.AsyncState;
            lock (conn) {
                try {
                    //为什么服务器Close一个conn之后，还能进来一次这个函数，且这时socket已经不存在？观察发现主动发出Close的一方会接收到一次多余的信息
                    int count = conn.socket.EndReceive(ar);

                    if (count <= 0) {
                        Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接1");
                        conn.Close();
                        return;
                    }

                    conn.buffCount += count;
                    ProcessData(conn);

                    conn.socket.BeginReceive(conn.readBuff,
                                            conn.buffCount,
                                            conn.BuffRemain(),
                                            SocketFlags.None,
                                            ReceiveCb,
                                            conn);
                }
                catch(Exception e){
                    Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接2 " + e.Message);
                    conn.Close();
                }
            }
        }

        private void HandleMsg(Conn conn, ProtocolBase protoBase) {
            string name = protoBase.GetName();
            string methodName = "Msg" + name;

            if (conn.status == Conn.Status.Connected || name == "HeartBeat" || name == "Logout") {
                //登录相关的消息处理
                MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
                if (mm == null) {
                    Console.WriteLine("[警告]HandleConnMsg未定义处理连接相关的方法：" + methodName);
                    return;
                }
                Object[] obj = new object[] { conn, protoBase };
                Console.WriteLine("[处理连接相关消息]" + conn.GetAddress() + "发起的" + methodName);
                mm.Invoke(handleConnMsg, obj);
            }   
            else {
                //游戏逻辑相关的消息处理
                MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
                if (mm == null) {
                    Console.WriteLine("[警告]HandlePlayerMsg未定义处理游戏逻辑相关的方法：" + methodName);
                    return;
                }
                Object[] obj = new object[] { conn.player, protoBase };
                Console.WriteLine("[处理游戏逻辑相关消息]" + conn.GetAddress() + "发起的" + methodName);
                mm.Invoke(handlePlayerMsg, obj);
            }

        }
        private void ProcessData(Conn conn) {
            if (conn.buffCount < sizeof(Int32))
                return;

            //尝试找出一条完整消息的长度
            Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
            conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);
            if (conn.buffCount < conn.msgLength + sizeof(Int32))
                return;

            //处理消息
            ProtocolBase protocol = proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
            HandleMsg(conn, protocol);

            //处理下一条消息
            int count = conn.buffCount - conn.msgLength - sizeof(Int32);
            Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
            conn.buffCount = count;
            if (conn.buffCount > 0)
                ProcessData(conn);
        }

        public void Send(Conn conn, ProtocolBase protocol) {
            byte[] bytes = protocol.Encode();     //为什么这里发送用的是UTF8？到底什么时候该用什么格式
            byte[] length = BitConverter.GetBytes((Int32)bytes.Length);
            byte[] sendBuff = length.Concat(bytes).ToArray();           //为什么还要ToArray？

            try {
                conn.socket.BeginSend(sendBuff,
                                      0,
                                      sendBuff.Length,
                                      SocketFlags.None,
                                      null,
                                      null);
            }
            catch (Exception e) {
                Console.WriteLine("[发送消息]" + conn.GetAddress() + " : " + e.Message);
            }
        }

        //对玩家进行广播
        public void Broadcast(ProtocolBase protocol) {
            for (int i = 0; i < conns.Length; ++i) {
                if (conns[i].status == Conn.Status.None)
                    continue;
                if (conns[i].status == Conn.Status.Connected)
                    continue;
                Send(conns[i], protocol);
            }
        }

        public void Close() {
            for (int i = 0; i < conns.Length; ++i) {
                Conn conn = conns[i];
                //if (conn == null)
                    //continue;
                if (conn.status == Conn.Status.None)
                    continue;
                lock (conn) {
                    conn.Close();
                }
            }
        }

        public void print() {
            Console.WriteLine("===服务器登录信息===");
            for (int i = 0; i < conns.Length; ++i) {
                //if (conns[i] == null)
                    //continue;
                if (conns[i].status == Conn.Status.None)
                    continue;

                string str = "连接[" + conns[i].GetAddress() + "] ";
                if (conns[i].status == Conn.Status.Login)
                    str += "玩家id " + conns[i].player.id;

                Console.WriteLine(str);
            }
        }
    }
}
