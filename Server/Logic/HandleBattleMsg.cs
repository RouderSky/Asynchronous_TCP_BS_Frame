using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Middle;
using Common;
using Server.Assistant;
using Server.Core;

namespace Server.Logic {
    public partial class HandlePlayerMsg {

        //开始战斗
        //参数：无
        //返回协议：-1代表失败，0代表成功
        public void MsgStartFight(Player player, ProtocolBase protocol) {
            //是否在房间内
            if (player.tempData.status != PlayerTempData.Statue.Room) {
                Console.WriteLine("MsgStartFight 玩家" + player.id + "并不在房间中");
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            //是否为房主
            if (!player.tempData.isOwner) {
                Console.WriteLine("MsgStartFight 玩家" + player.id + "不是房主");
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            Room room = player.tempData.room;
            if (!room.CanStartFight()) {
                Console.WriteLine("MsgStartFight 房间不满足开站条件");
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            protocol.AddInt(0);
            player.Send(protocol);
            room.StartFight();
        }

        //收到transform信息
        //参数：posX，posY，posZ，rotX，rotY，rotZ，gunRot，gunRoll
        //返回协议：广播 id，posX，posY，posZ，rotX，rotY，rotZ，gunRot，gunRoll
        public void MsgUpdateUnitInfo(Player player, ProtocolBase protocol) {
            if (player.tempData.status != PlayerTempData.Statue.Fight)
                return;

            int start = 0;
            string protoName = protocol.GetString(start, ref start);
            float posX = protocol.GetFloat(start, ref start);
            float posY = protocol.GetFloat(start, ref start);
            float posZ = protocol.GetFloat(start, ref start);
            float rotX = protocol.GetFloat(start, ref start);
            float rotY = protocol.GetFloat(start, ref start);
            float rotZ = protocol.GetFloat(start, ref start);

            float gunRot = protocol.GetFloat(start, ref start);
            float gunRoll = protocol.GetFloat(start, ref start);

            Room room = player.tempData.room;

            //作弊校验
            //略

            //为什么tempData中只保留了一部分数据，为了校验？？？
            player.tempData.posX = posX;
            player.tempData.posY = posY;
            player.tempData.posZ = posZ;
            player.tempData.lastShootTime = Sys.GetTimeStamp();

            //广播
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("UpdateUnitInfo");
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(posX);
            protocolRet.AddFloat(posY);
            protocolRet.AddFloat(posZ);
            protocolRet.AddFloat(rotX);
            protocolRet.AddFloat(rotY);
            protocolRet.AddFloat(rotZ);
            protocolRet.AddFloat(gunRot);
            protocolRet.AddFloat(gunRoll);
            room.Broadcast(protocolRet);
        }
    }
}
