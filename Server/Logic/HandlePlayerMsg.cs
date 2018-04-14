using System;

using Common;
using Server.Core;
using Server.Middle;

namespace Server.Logic {
    public partial class HandlePlayerMsg {

        //获取分数
        //无参数
        //返回协议：int分数
        public void MsgGetScore(Player player, ProtocolBase protocol) {
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("GetScore");
            protocolRet.AddInt(player.data.score);
            player.Send(protocolRet);
            Console.WriteLine("MsgGetScore " + player.id + " " + player.data.score.ToString());
        }

        //增加分数
        //无参数
        //返回协议：无
        public void MsgAddScore(Player player, ProtocolBase protocol) {
            player.data.score += 1;

            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            Console.WriteLine("MsgAddScore " + player.id + " " + player.data.score.ToString());
        }

        //获取场景的所有Avatar信息
        //无参数
        //返回协议：场景avatar数，id，x，y，z，id，x，y，z，id，x，y，z，...
        public void MsgGetList(Player player, ProtocolBase protocol) {
            Scene.instance.SendAvatarList(player);
        }

        //更新avatar的信息
        //协议参数：x, y, z, score
        public void MsgUpdateInfo(Player player, ProtocolBase protocol) {
            int start = 0;
            //ProtocolBytes protocol = (ProtocolBytes)proto;
            string protoName = protocol.GetString(start, ref start);
            float x = protocol.GetFloat(start, ref start);
            float y = protocol.GetFloat(start, ref start);
            float z = protocol.GetFloat(start, ref start);
            int score = player.data.score;
            Scene.instance.UpdateInfo(player.id, x, y, z, score);
        }
    }
}
