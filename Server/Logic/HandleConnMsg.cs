using System;

using Common;
using Server.Core;
using Server.Middle;
using Assistant;

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
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            Console.WriteLine("[收到注册协议]" + conn.GetAddress() + "发起的 " + "用户名：" + id + " 密码：" + pw);

            bool ret = DataMgr.instance.Register(id, pw);

            //返回协议
            ProtocolBase protocolBack = ServNet.instance.proto.Decode(null, 0, 0);
            protocolBack.AddString("Register");
            if (ret)
                protocolBack.AddInt(0);
            else
                protocolBack.AddInt(-1);
            ServerSystem.instance.Send(conn, protocolBack);

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
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            Console.WriteLine("[收到登录协议]" + conn.GetAddress() + "发起的 " + "用户名：" + id + " 密码：" + pw);

            //检查密码
            bool ret = DataMgr.instance.CheckPassWord(id, pw);
            ProtocolBase protocolBack = ServNet.instance.proto.Decode(null, 0, 0);
            protocolBack.AddString("Login");
            if (!ret) {
                protocolBack.AddInt(-1);
                ServerSystem.instance.Send(conn, protocolBack);
                return;
            }

            //检查是否已经登录了
            ret = Player.KickOff(id);
            if (!ret) {
                protocolBack.AddInt(-2);
                ServerSystem.instance.Send(conn, protocolBack);
                return;
            }
        
            //获取玩家数据
            PlayerData playerData = DataMgr.instance.GetPlayerData(id);
            if (playerData == null) {
                protocolBack.AddInt(-3);
                ServerSystem.instance.Send(conn, protocolBack);
                return;
            }

            Player player = new Player(id, conn);
            ServerSystem.instance.ConnLogin(conn, player, playerData);

            //事件
            ServNet.instance.handlePlayerEvent.OnLogin(conn.player);

            protocolBack.AddInt(0);
            ServerSystem.instance.Send(conn, protocolBack);
        }

        //登出
        //协议参数：无
        //返回协议：0代表成功，-1代表失败
        public void MsgLogout(Conn conn, ProtocolBase protocol) {
            ProtocolBase protocolBack = ServNet.instance.proto.Decode(null, 0, 0);
            protocolBack.AddString("Logout");
            if (conn.player.Logout())
                protocolBack.AddInt(0);
            else
                protocolBack.AddInt(-1);

            ServerSystem.instance.Send(conn, protocolBack);
        }

    }
}
