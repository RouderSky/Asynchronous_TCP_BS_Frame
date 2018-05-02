using System;

using Common;
using Server.Core;
using Server.Middle;

namespace Server.Logic {
    public partial class HandlePlayerMsg {

        //获取分数
        //无参数
        //返回协议：int分数
        public void MsgGetMaxScore(Player player, ProtocolBase protocol) {
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("GetMaxScore");
            protocolRet.AddInt(player.data.maxScore);
            player.Send(protocolRet);
            Console.WriteLine("MsgGetMaxScore " + player.id + " " + player.data.maxScore.ToString());
        }

        //增加分数
        //无参数
        //返回协议：无
        public void MsgAddMaxScore(Player player, ProtocolBase protocol) {
            player.data.maxScore += 1;

            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            Console.WriteLine("MsgAddMaxScore " + player.id + " " + player.data.maxScore.ToString());
        }
    }
}
