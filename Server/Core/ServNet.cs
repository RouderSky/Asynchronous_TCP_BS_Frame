﻿using System;
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

namespace Server.Core {
    class ServNet {
        public Socket listenfd;

        public int maxConn = 50;        
        public Conn[] conns;

        public static ServNet instance;

        public ServNet() {
            instance = this;        //这是单例模式？？？
        }

        public int NewIndex() {
            if (conns == null)
                return -1;
            for (int i = 0; i < conns.Length; ++i) {
                if (conns[i].isUse == false) {
                    return i;
                }
            }
            return -1;
        }

        //启动服务器
        public void Start(string host, int port) {
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
                    int count = conn.socket.EndReceive(ar);
                    if (count <= 0) {
                        Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接");
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
                catch {
                    Console.WriteLine("收到 [" + conn.GetAddress() + "] 断开连接");
                    conn.Close();
                }
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

            //解析出真正的消息
            string str = System.Text.Encoding.UTF8.GetString(conn.readBuff, sizeof(Int32), conn.msgLength);
            Console.WriteLine("收到消息 [" + conn.GetAddress() + "] " + "数据包长度：" + conn.msgLength + sizeof(Int32) + "内容：" + str);

            //处理消息
            Send(conn, str);
            //HandleMsg(conn, str)

            //处理下一条消息
            int count = conn.buffCount - conn.msgLength - sizeof(Int32);
            Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
            conn.buffCount = count;
            if (conn.buffCount > 0)
                ProcessData(conn);
        }

        public void Send(Conn conn, string str) {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);     //为什么这里发送用的是UTF8？到底什么时候该用什么格式
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
