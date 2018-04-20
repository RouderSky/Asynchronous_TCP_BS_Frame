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

        public int maxPlayers = 6;
        public Dictionary<string, Player> playerList = new Dictionary<string, Player>();      //字典有序的吗？是不是先Add在遍历Value的时候先被遍历到？？？

        public bool AddPlayer(Player player) {
            lock (playerList) {
                if (playerList.Count >= maxPlayers)
                    return false;
                    
                PlayerTempData tempData = player.tempData;
                tempData.room = this;
                tempData.team = SwitchTeam();   
                tempData.status = PlayerTempData.Statue.Room;

                string id = player.id;
                playerList.Add(id, player);
            }
            return true;
        }
        //自动分配阵容
        public int SwitchTeam() {
            int count1 = 0;
            int count2 = 0;
            foreach (Player player in playerList.Values) {
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

            lock (playerList) {
                if (!playerList.ContainsKey(player.id))
                    return;
                bool isOwner = playerList[player.id].tempData.isOwner;
                playerList[player.id].tempData.status = PlayerTempData.Statue.Lobby;
                playerList.Remove(player.id);
                if (isOwner)
                    UpdateOwner();      //不太合理.................
            }

            lock (playerList) {
                if (playerList.Count == 0) {
                    lock (RoomMgr.instance.roomList) {
                        RoomMgr.instance.roomList.Remove(this);
                    }
                }
            }
        }

        //将目前最新进入房间的玩家设置为房主
        //不太合理.......................
        public void UpdateOwner() {
            lock (playerList) {
                if (playerList.Count <= 0)
                    return;

                foreach (Player player in playerList.Values)
                    player.tempData.isOwner = false;

                Player p = playerList.Values.First();
                p.tempData.isOwner = true;
            }
        }

        public void Broadcast(ProtocolBase protocol) {
            foreach (Player player in playerList.Values) {
                player.Send(protocol);
            }
        }

        public ProtocolBase GetRoomInfoBack() {
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetRoomInfo");
            protocol.AddInt(playerList.Count);
            foreach (Player p in playerList.Values) {
                protocol.AddString(p.id);
                protocol.AddInt(p.tempData.team);
                protocol.AddInt(p.data.maxScore);
                protocol.AddInt(p.tempData.isOwner ? 1 : 0);
            }
            return protocol;
        }
    }
}
