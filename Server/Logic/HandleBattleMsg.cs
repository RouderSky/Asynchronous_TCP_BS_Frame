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
            RoomSystem.instance.StartFightForRoom(room);
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
            player.tempData.lastUpdatePosTime = Sys.GetTimeStamp();

            //加上id后广播
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
            RoomSystem.instance.BroadcastInRoom(room, protocolRet);
        }

        //收到发射炮弹请求
        //参数：posX，posY，posZ，rotX，rotY，rotZ
        //返回协议：广播 id，posX，posY，posZ，rotX，rotY，rotZ
        public void MsgShooting(Player player, ProtocolBase protocol) {
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

            //加上id后广播
            Room room = player.tempData.room;
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("Shooting");
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(posX);
            protocolRet.AddFloat(posY);
            protocolRet.AddFloat(posZ);
            protocolRet.AddFloat(rotX);
            protocolRet.AddFloat(rotY);
            protocolRet.AddFloat(rotZ);
            RoomSystem.instance.BroadcastInRoom(room, protocolRet);
        }

        //伤害
        //参数：被击者id，伤害数值damage
        //返回协议：广播 攻击者id，被击者id，伤害数值damage
        public void MsgHit(Player player, ProtocolBase protocol) {
            if (player.tempData.status != PlayerTempData.Statue.Fight)
                return;

            //作弊校验
//             long lastShootTime = player.tempData.lastShootTime;
//             if (Sys.GetTimeStamp() - lastShootTime < 1) {
//                 Console.WriteLine("MsgHit 开炮作弊 " + player.id);
//                 return;
//             }
//             player.tempData.lastShootTime = Sys.GetTimeStamp();

            int start = 0;
            string protoName = protocol.GetString(start, ref start);
            string enenmyID = protocol.GetString(start, ref start);
            float damage = protocol.GetFloat(start, ref start);

            //扣除生命值
            Room room = player.tempData.room;
            lock (room.playerDict) {
                if (!room.playerDict.ContainsKey(enenmyID)) {
                    Console.WriteLine("MsgHit not Contains enemy " + enenmyID);
                    return;
                }
                Player enemy = room.playerDict[enenmyID];
                //             if (enemy == null)
                //                 return;
                if (enemy.tempData.hp <= 0)
                    return;
                enemy.tempData.hp -= damage;
                Console.WriteLine("MsgHit " + enenmyID + " hp:" + enemy.tempData.hp);
            }

            //广播
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("Hit");
            protocolRet.AddString(player.id);
            protocolRet.AddString(enenmyID);
            protocolRet.AddFloat(damage);
            RoomSystem.instance.BroadcastInRoom(room, protocolRet);

            //胜负判断
            RoomSystem.instance.DealWithWinForRoom(room);
        }

        //玩家拾取盒子
        //参数：盒子id
        //返回协议：0代表成功，-1代表失败
        //          广播 要删除的Box的ID
        public void MsgPlayerAvatarGetBox(Player player, ProtocolBase protocol) {
            int start = 0;
            string protoName = protocol.GetString(start, ref start);
            string boxID = protocol.GetString(start, ref start);

            bool flag = false;
            lock (player.tempData.room.boxSet) {
                if (player.tempData.room.boxSet.Contains(boxID)) {      //J...
                    player.tempData.room.boxSet.Remove(boxID);          //J...
                    flag = true;
                }
            }

            ProtocolBase protocolBack = ServNet.instance.proto.Decode(null, 0, 0);
            protocolBack.AddString("PlayerAvatarGetBox");
            if (flag){
                protocolBack.AddInt(0);

                ProtocolBase protocolBroadcast = ServNet.instance.proto.Decode(null, 0, 0);
                protocolBroadcast.AddString("DelBox");
                protocolBroadcast.AddString(boxID);
                RoomSystem.instance.BroadcastInRoom(player.tempData.room, protocolBroadcast);
            }
            else{
                protocolBack.AddInt(-1);
            }
           player.Send(protocolBack);
        }

        //释放技能
        //参数：posX，posY，posZ，rotX，rotY，rotZ，skillType
        //返回协议：广播 释放者id，posX，posY，posZ，rotX，rotY，rotZ，skillType
        public void MsgReleaseSkill(Player player, ProtocolBase protocol) {
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
            int skillType = protocol.GetInt(start, ref start);

            //加上id后广播
            Room room = player.tempData.room;
            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("ReleaseSkill");
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(posX);
            protocolRet.AddFloat(posY);
            protocolRet.AddFloat(posZ);
            protocolRet.AddFloat(rotX);
            protocolRet.AddFloat(rotY);
            protocolRet.AddFloat(rotZ);
            protocolRet.AddInt(skillType);
            RoomSystem.instance.BroadcastInRoom(room, protocolRet);
        }
    }
}
