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
            /*
            PlayerTempData tempData = player.tempData;
            if (tempData.status == PlayerTempData.Statue.Lobby)
                return;*/

            lock (playerDict) {
                if (!playerDict.ContainsKey(player.id))
                    return;
                bool isOwner = playerDict[player.id].tempData.isOwner;
                playerDict[player.id].tempData.status = PlayerTempData.Statue.Lobby;
                playerDict.Remove(player.id);

                if (isOwner) {
                    if (playerDict.Count <= 0)
                        return;

                    foreach (Player playerTemp in playerDict.Values)
                        playerTemp.tempData.isOwner = false;

                    Player p = playerDict.Values.First();
                    p.tempData.isOwner = true;
                }
            }

            lock (playerDict) {
                if (playerDict.Count == 0) {
                    lock (RoomMgr.instance.roomList) {
                        RoomMgr.instance.roomList.Remove(this);
                    }
                }
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
                int teamPos2 = 1;
                foreach (Player p in playerDict.Values) {
                    p.tempData.hp = 200;            //服务端玩家数据初始化
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

        int IsWin() {
            if (status != Status.Fight)
                return 0;

            int count1 = 0;
            int count2 = 0;
            foreach (Player player in playerDict.Values) {
                PlayerTempData pt = player.tempData;
                if (pt.team == 1 && pt.hp > 0) 
                    count1++;
                if (pt.team == 2 && pt.hp > 0)
                    count2++;
            }

            if (count1 == 0)
                return 2;
            if (count2 == 0)
                return 1;
            return 0;
        }

        public void UpdateWin() {
            int isWin = IsWin();
            if (isWin == 0)
                return;

            status = Status.Prepare;        //原本放在临界区内的
            lock (playerDict) {
                foreach (Player player in playerDict.Values) {
                    player.tempData.status = PlayerTempData.Statue.Room;        //不对，玩家可能还在战场中；但是为什么还能正常运行？？？......................
                    if (player.tempData.team == isWin)
                        player.data.maxScore++;     //只是模拟下更新数据，没有实际意义
                }
            }

            //广播
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("Result");
            protocol.AddInt(isWin);
            Broadcast(protocol);
        }

    }
}
