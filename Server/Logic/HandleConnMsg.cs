﻿using System;

using Common;
using Server.Core;
using Server.Middle;
using Server.Assistant;

namespace Server.Logic {
    //partial关键字表明这个类定义并不完整，只是一部分
    public partial class HandleConnMsg {
        //心跳
        //协议参数：无
        //返回协议：无
        public void MsgHeartBeat(Conn conn, ProtocolBase proto) {
            conn.lastTickTime = Sys.GetTimeStamp();
            Console.WriteLine("[更新心跳时间]" + conn.GetAddress());
        }

        //注册
        //协议参数：用户名，密码
        //返回协议：-1代表失败，0代表成功

        public void MsgRegister(Conn conn, ProtocolBase protocol) {
            
            //解析出所有参数
            int start = 0;
            //ProtocolBytes protocol = (ProtocolBytes)proto;          //不转不行吗？？？
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            Console.WriteLine("[收到注册协议]" + conn.GetAddress() + "发起的 " + "用户名：" + id + " 密码：" + pw);

            bool ret = DataMgr.instance.Register(id, pw);

            //通知客户端注册结果
            protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("Register");
            if (ret)
                protocol.AddInt(0);
            else
                protocol.AddInt(-1);
            conn.Send(protocol);

            //创建角色:一个账号对应一个角色的模式
            if (ret) {
                DataMgr.instance.CreatePlayer(id);
            }
        }

        //登录
        //协议参数：用户名，密码
        //返回协议：-1密码错误，-2顶下线失败，-3读取角色数据失败，0成功
        public void MsgLogin(Conn conn, ProtocolBase protocol) {
            //解析出所有参数
            int start = 0;
            //ProtocolBytes protocol = (ProtocolBytes)proto;      //不转不行吗？？？
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            Console.WriteLine("[收到登录协议]" + conn.GetAddress() + "发起的 " + "用户名：" + id + " 密码：" + pw);

            //检查密码
            bool ret = DataMgr.instance.CheckPassWord(id, pw);
            protocol = ServNet.instance.proto.Decode(null, 0, 0);        //发送信息时竟然要手动new出指定的协议类型，不能由ServNet统一确定吗？？？
            protocol.AddString("Login");
            if (!ret) {
                protocol.AddInt(-1);
                conn.Send(protocol);
                return;
            }

            //检查是否已经登录了
            ProtocolBase protocolLogout = ServNet.instance.proto.Decode(null, 0, 0);
            protocolLogout.AddString("ForceLogout");
            ret = Player.KickOff(id, protocolLogout);
            if (!ret) {
                protocol.AddInt(-2);
                conn.Send(protocol);
                return;
            }
        
            //获取玩家数据
            PlayerData playerData = DataMgr.instance.GetPlayerData(id);
            if (playerData == null) {
                protocol.AddInt(-3);
                conn.Send(protocol);
                return;
            }
            conn.player = new Player(id, conn);
            conn.player.data = playerData;

            //事件
            ServNet.instance.handlePlayerEvent.OnLogin(conn.player);

            protocol.AddInt(0);
            conn.Send(protocol);
        }

        //登出
        //协议参数：无
        //返回协议：无
        public void MsgLogout(Conn conn, ProtocolBase protocol) {
            conn.Close();
        }


    }
}
