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

using Server.Assistant;

//读conn的时候不需要lock吗？其实是一个读写者问题
//当前，要发送数据时，还是要手动new一个ProtoBytes，工厂不能new.............

namespace Server.Core {
    class ServNet {
        public Socket listenfd;

        public int maxConn = 50;        
        public Conn[] conns;

        public static ServNet instance;

        //心跳处理
        System.Timers.Timer timer = new System.Timers.Timer(1000);      //检测用定时器
        public long heartBeatTime = 10;        //最大通信间隔，单位是秒，有客户端超过这个时间没有通信过就断开

        //协议处理
        //由用户启动服务器的时候设置
        public ProtocolBase proto;      //只是为了用来解码构造出一个协议信息，本身没有具体的消息

        public ServNet() {
            instance = this;        //这是单例模式？？？
        }

        public int NewIndex() {
            if (conns == null)
                return -1;
            for (int i = 0; i < conns.Length; ++i) {
                if (conns[i].isUse == false) {      //从这里可以看出，当前线程并不安全；有可能两个客户端取到相同的index，也就是说有一个客户端会逻辑上连接失败
                    return i;
                }
            }
            return -1;
        }

        public void HeartBeat() {
            Console.WriteLine("[主定时器执行]");
            long timeNow = Sys.GetTimeStamp();

            for (int i = 0; i < conns.Length; ++i) {
                Conn conn = conns[i];
                if (conn == null)       //不可能
                    continue;
                if (!conn.isUse)
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
            timer.Start();
        }
        //启动服务器
        public void Start(string host, int port) {
            //心跳处理用定时器
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;       //这样可以启动定时器？和Start的功能完全一样？

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

                int index = NewIndex();

                if (index < 0) {
                    socket.Close();
                    Console.WriteLine("[警告]连接已满");
                }
                else {
                    Conn conn = conns[index];
                    conn.Init(socket);
                    string adr = conn.GetAddress();
                    Console.WriteLine("客户端连接 [" + adr + "] conn池ID：" + index);

                    conn.socket.BeginReceive(conn.readBuff,
                                             conn.buffCount,
                                             conn.BuffRemain(),
                                             SocketFlags.None,
                                             ReceiveCb,
                                             conn);

                    //继续接收客户端连接
                    listenfd.BeginAccept(AcceptCb, null);
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
                    //为什么服务器Close一个conn之后，还能进来一次这个函数，且这时socket已经不存在？
                    int count = conn.socket.EndReceive(ar);

                    if (count <= 0) {
                        Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接1");
                        conn.Close();
                        return;
                    }

                    conn.buffCount += count;    //不用担心缓冲区不够长吗？？？
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
            Console.WriteLine("[收到协议]" + name);

            //处理具体消息
            //这里完全是双方用文字协定好解析信息的方法
            if (name == "HeartBeat") {
                Console.WriteLine("[更新心跳时间]" + conn.GetAddress());
                conn.lastTickTime = Sys.GetTimeStamp();
            }

            Send(conn, protoBase);      //为什么要传回去？应该仅做测试用
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

        public void Broadcast(ProtocolBase protocol) {
            for (int i = 0; i < conns.Length; ++i) {
                if (!conns[i].isUse)
                    continue;
                if (conns[i].player == null)    //怎么不判断conns[i]本身了？？？
                    continue;
                Send(conns[i], protocol);
            }
        }

        public void Close() {
            for (int i = 0; i < conns.Length; ++i) {
                Conn conn = conns[i];
                if (conn == null)
                    continue;
                if (!conn.isUse)
                    continue;
                lock (conn) {
                    conn.Close();
                }
            }
        }

    }
}
