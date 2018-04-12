using System;

using Server.Core;
using Server.Middle;

namespace Server.Logic {
    public partial class HandlePlayerMsg {

        //获取分数
        //无参数
        //返回协议：int分数
        public void MsgGetScore(Player player, ProtocolBase protoBase) {
            ProtocolBytes protocolRet = new ProtocolBytes();       //发送信息时竟然要手动new出指定的协议类型，不能由ServNet统一确定吗？？？
            protocolRet.AddString("GetScore");
            protocolRet.AddInt(player.data.score);
            player.Send(protocolRet);
            Console.WriteLine("MsgGetScore " + player.id + " " + player.data.score.ToString());
        }

        //增加分数
        //无参数
        //返回协议：无
        public void MsgAddScore(Player player, ProtocolBase protoBase) {
            player.data.score += 1;

            ProtocolBytes protocolRet = new ProtocolBytes();       //发送信息时竟然要手动new出指定的协议类型，不能由ServNet统一确定吗？？？
            protocolRet.AddString("AddScore");
            player.Send(protocolRet);
            Console.WriteLine("MsgAddScore " + player.id + " " + player.data.score.ToString());
        }
    }
}
