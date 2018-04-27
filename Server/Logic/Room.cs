using System;
using System.Collections.Generic;
using System.Linq;

using Server.Core;
using Server.Middle;
using Server.Logic;
using Common;

namespace Server.Logic {
    public class Room {
        public enum Status {
            Prepare = 1,
            Fight = 2,
        }
        public Status status = Status.Prepare;

        //注意线程安全
        public int maxPlayers = 6;
        public Dictionary<string, Player> playerDict = new Dictionary<string, Player>();      //字典有序的吗？是不是先Add在遍历Value的时候先被遍历到？？？

        public bool AddPlayer(Player player) {
            lock (playerDict) {
                if (playerDict.Count >= maxPlayers)
                    return false;
                    
                PlayerTempData tempData = player.tempData;
                tempData.room = this;
                tempData.team = SwitchTeam();   
                tempData.status = PlayerTempData.Statue.Room;

                string id = player.id;
                playerDict.Add(id, player);
            }
            return true;
        }
        //自动分配阵容
        public int SwitchTeam() {
            int count1 = 0;
            int count2 = 0;
            foreach (Player player in playerDict.Values) {
                if (player.tempData.team == 1)
                    count1++;
                if (player.tempData.team == 2)
                    count2++;
            }
            if (count1 <= count2)
                return 1;
            else
                return 2;
        }

        public void DelPlayer(Player player) {
            PlayerTempData tempData = player.tempData;
            if (tempData.status == PlayerTempData.Statue.Lobby)
                return;

            lock (playerDict) {
                if (!playerDict.ContainsKey(player.id))
                    return;
                bool isOwner = playerDict[player.id].tempData.isOwner;
                playerDict[player.id].tempData.status = PlayerTempData.Statue.Lobby;
                playerDict.Remove(player.id);
                if (isOwner)
                    UpdateOwner();      //不太合理.................
            }

            lock (playerDict) {
                if (playerDict.Count == 0) {
                    lock (RoomMgr.instance.roomList) {
                        RoomMgr.instance.roomList.Remove(this);
                    }
                }
            }
        }

        //将目前最新进入房间的玩家设置为房主
        //不太合理.......................
        public void UpdateOwner() {
            lock (playerDict) {
                if (playerDict.Count <= 0)
                    return;

                foreach (Player player in playerDict.Values)
                    player.tempData.isOwner = false;

                Player p = playerDict.Values.First();
                p.tempData.isOwner = true;
            }
        }

        public void Broadcast(ProtocolBase protocol) {
            foreach (Player player in playerDict.Values) {
                player.Send(protocol);
            }
        }

        public ProtocolBase GetRoomInfoBack() {
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetRoomInfo");
            protocol.AddInt(playerDict.Count);
            foreach (Player p in playerDict.Values) {
                protocol.AddString(p.id);
                protocol.AddInt(p.tempData.team);
                protocol.AddInt(p.data.maxScore);
                protocol.AddInt(p.tempData.isOwner ? 1 : 0);
            }
            return protocol;
        }

        public bool CanStartFight() {
            if (status != Status.Prepare)
                return false;

            int count1 = 0;
            int count2 = 0;

            foreach (Player player in playerDict.Values) {
                if (player.tempData.team == 1)
                    count1++;
                if (player.tempData.team == 2)
                    count2++;
            }

            if (count1 < 1 || count2 < 1)
                return false;

            return true;
        }

        public void StartFight() {
            status = Status.Fight;

            //生成协议
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("Fight");
            lock (playerDict) {
                protocol.AddInt(playerDict.Count);
                int teamPos1 = 1;
                int teamPos2 = 2;
                foreach (Player p in playerDict.Values) {
                    p.tempData.hp = 200;
                    protocol.AddString(p.id);
                    protocol.AddInt(p.tempData.team);
                    if (p.tempData.team == 1)
                        protocol.AddInt(teamPos1++);
                    else
                        protocol.AddInt(teamPos2++);
                    p.tempData.status = PlayerTempData.Statue.Fight;
                }
            }

            //广播
            Broadcast(protocol);
        }
    }
}
