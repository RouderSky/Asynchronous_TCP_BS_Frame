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
using Server.Middle;

namespace Server.Core {

    //要求单例.......................
    class ServerSystem {

        public static ServerSystem instance;

        public ServerSystem() {
            instance = this;
        }

#region Conn
        public void InitConn(Conn conn, Socket socket) {
            conn.socket = socket;
            conn.buffCount = 0;
            conn.lastTickTime = Sys.GetTimeStamp();
            conn.status = Conn.Status.Connected;
        }

        public void ConnLogin(Conn conn, Player player, PlayerData playerData) {
            conn.status = Conn.Status.Login;
            conn.player = player;
            conn.player.data = playerData;
        }

        public void CloseConn(Conn conn) {
            if (conn.status == Conn.Status.None)
                return;
            if (conn.status == Conn.Status.Login) {
                if (!conn.player.Logout()) {
                    Console.WriteLine("玩家数据保存失败，无法关闭连接");
                    return;
                }
                conn.status = Conn.Status.Connected;
            }
            Console.WriteLine("[断开连接]" + conn.GetAddress());

            conn.socket.Shutdown(SocketShutdown.Both);       //？？？
            conn.socket.Close();
            conn.status = Conn.Status.None;
        }
#endregion

        void HandleMainTimer(object sender, System.Timers.ElapsedEventArgs e) {
            //扫描所有的连接
            long timeNow = Sys.GetTimeStamp();
            for (int i = 0; i < World.instance.conns.Length; ++i) {
                Conn conn = World.instance.conns[i];

                if (conn.status == Conn.Status.None)
                    continue;

                if (timeNow - conn.lastTickTime > ServNet.instance.maxHeartBeatInterval) {
                    Console.WriteLine("[心跳引起断开连接]" + conn.GetAddress());
                    lock (conn)
                        ServerSystem.instance.CloseConn(conn);
                }
            }

            ServNet.instance.timer.Start();      //这么做是确保上一次检测已经完成再开始下一次？？？
        }
        //启动服务器
        public void Start(string host, int port) {

            World.instance.conns = new Conn[ServNet.instance.maxConn];
            for (int i = 0; i < ServNet.instance.maxConn; ++i) {
                World.instance.conns[i] = new Conn();
            }

            //心跳处理用定时器
            ServNet.instance.timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            ServNet.instance.timer.AutoReset = false;
            ServNet.instance.timer.Enabled = true;       //这样可以启动定时器？和Start的功能完全一样？？？

            //Socket
            ServNet.instance.listenfd = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream,
                                  ProtocolType.Tcp);

            //Bind
            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, port);
            ServNet.instance.listenfd.Bind(ipEp);

            //Listen
            ServNet.instance.listenfd.Listen(ServNet.instance.maxConn);

            ServNet.instance.listenfd.BeginAccept(AcceptCb, null);

            Console.WriteLine("[服务器]启动成功");
        }

        void AcceptCb(IAsyncResult ar) {
            try {
                Socket socket = ServNet.instance.listenfd.EndAccept(ar);

                for (int i = 0; i < World.instance.conns.Length; ++i) {
                    lock (World.instance.conns[i]) {
                        if (World.instance.conns[i].status == Conn.Status.None) {
                            Conn conn = World.instance.conns[i];
                            ServerSystem.instance.InitConn(conn, socket);
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
                            ServNet.instance.listenfd.BeginAccept(AcceptCb, null);

                            return;
                        }
                    }
                }

            }
            catch (Exception e) {
                Console.WriteLine("AcceptCb失败：" + e.Message);
            }
        }

        void ReceiveCb(IAsyncResult ar) {
            Conn conn = (Conn)ar.AsyncState;
            lock (conn) {
                try {
                    //为什么服务器Close一个conn之后，还能进来一次这个函数，且这时socket已经不存在？观察发现主动发出Close的一方会接收到一次多余的信息
                    int count = conn.socket.EndReceive(ar);

                    if (count <= 0) {
                        Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接1");
                        ServerSystem.instance.CloseConn(conn);
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
                    ServerSystem.instance.CloseConn(conn);
                }
            }
        }

        void HandleMsg(Conn conn, ProtocolBase protoBase) {
            string name = protoBase.GetName();
            string methodName = "Msg" + name;

            if (conn.status == Conn.Status.Connected || name == "HeartBeat" || name == "Logout") {
                //登录相关的消息处理
                MethodInfo mm = ServNet.instance.handleConnMsg.GetType().GetMethod(methodName);
                if (mm == null) {
                    Console.WriteLine("[警告]HandleConnMsg未定义处理连接相关的方法：" + methodName);
                    return;
                }
                Object[] obj = new object[] { conn, protoBase };
                //Console.WriteLine("[处理连接相关消息]" + conn.GetAddress() + "发起的" + methodName);
                mm.Invoke(ServNet.instance.handleConnMsg, obj);
            }   
            else {
                //游戏逻辑相关的消息处理
                MethodInfo mm = ServNet.instance.handlePlayerMsg.GetType().GetMethod(methodName);
                if (mm == null) {
                    Console.WriteLine("[警告]HandlePlayerMsg未定义处理游戏逻辑相关的方法：" + methodName);
                    return;
                }
                Object[] obj = new object[] { conn.player, protoBase };
                //Console.WriteLine("[处理游戏逻辑相关消息]" + conn.GetAddress() + "发起的" + methodName);
                mm.Invoke(ServNet.instance.handlePlayerMsg, obj);
            }

        }
        void ProcessData(Conn conn) {
            if (conn.buffCount < sizeof(Int32))
                return;

            //尝试找出一条完整消息的长度
            Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
            conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);
            if (conn.buffCount < conn.msgLength + sizeof(Int32))
                return;

            //处理消息
            ProtocolBase protocol = ServNet.instance.proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
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

        public void Broadcast(ProtocolBase protocol) {
            for (int i = 0; i < World.instance.conns.Length; ++i) {
                if (World.instance.conns[i].status == Conn.Status.None)
                    continue;
                if (World.instance.conns[i].status == Conn.Status.Connected)
                    continue;
                Send(World.instance.conns[i], protocol);
            }
        }

        public void Close() {
            if (World.instance.conns[0] == null)   //未启动服务器
                return;

            for (int i = 0; i < World.instance.conns.Length; ++i) {
                Conn conn = World.instance.conns[i];
                if (conn.status == Conn.Status.None)
                    continue;
                lock (conn) {
                    ServerSystem.instance.CloseConn(conn);       //为什么还要这样关闭，直接清空conns数组不就好了？？？
                }
            }
        }

        public void print() {
            if (World.instance.conns[0] == null)   //未启动服务器
                return;

            Console.WriteLine("===服务器登录信息===");
            for (int i = 0; i < World.instance.conns.Length; ++i) {
                if (World.instance.conns[i].status == Conn.Status.None)
                    continue;
                string str = "连接[" + World.instance.conns[i].GetAddress() + "] ";
                if (World.instance.conns[i].status == Conn.Status.Login)
                    str += "玩家id " + World.instance.conns[i].player.id;

                Console.WriteLine(str);
            }
        }
    }
}
