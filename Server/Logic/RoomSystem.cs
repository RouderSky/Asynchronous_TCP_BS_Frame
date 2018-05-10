using System;
using System.Collections.Generic;
using System.Linq;

using Common;
using Server.Core;
using Server.Middle;

namespace Server.Logic {
    //要求单例...
    public class RoomSystem {
        public static RoomSystem instance;
        public RoomSystem() {
            instance = this;
        }

        public void CreateRoom(Player player) {
            Room room = new Room();
            lock (World.instance.roomList) {
                World.instance.roomList.Add(room);
                AddPlayerToRoom(room, player);
            }
        }

        public ProtocolBase GetRoomListBack() {
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetRoomList");
            protocol.AddInt(World.instance.roomList.Count);
            for (int i = 0; i < World.instance.roomList.Count; ++i) {
                Room room = World.instance.roomList[i];
                protocol.AddInt(room.playerDict.Count);
                protocol.AddInt((int)room.status);
            }
            return protocol;
        }

        public void DealWithWinForRoom(Room room) {
            int isWin = room.IsWin();
            if (isWin == 0)
                return;

            room.status = Room.Status.Prepare;        //原本放在临界区内的
            lock (room.playerDict) {
                foreach (Player player in room.playerDict.Values) {
                    player.tempData.status = PlayerTempData.Statue.Room;        //不对，玩家可能还在战场中...
                    if (player.tempData.team == isWin)
                        player.data.maxScore++;     //只是模拟下更新数据，没有实际意义
                }
            }

            //广播
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("Result");
            protocol.AddInt(isWin);
            BroadcastInRoom(room, protocol);
        }

        public bool AddPlayerToRoom(Room room, Player player) {
            lock (room.playerDict) {
                if (room.playerDict.Count >= room.maxPlayers)
                    return false;

                PlayerTempData tempData = player.tempData;
                tempData.room = room;
                tempData.team = SwitchTeamForRoom(room);
                tempData.status = PlayerTempData.Statue.Room;
                tempData.isOwner = false;
                if (room.playerDict.Count == 0)
                    tempData.isOwner = true;

                string id = player.id;
                room.playerDict.Add(id, player);
            }
            return true;
        }
        //自动分配阵容
        public int SwitchTeamForRoom(Room room) {
            int count1 = 0;
            int count2 = 0;
            foreach (Player player in room.playerDict.Values) {
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

        public void DelPlayerForRoom(Room room, Player player) {
            /*
            PlayerTempData tempData = player.tempData;
            if (tempData.status == PlayerTempData.Statue.Lobby)
                return;*/

            lock (room.playerDict) {
                if (!room.playerDict.ContainsKey(player.id))
                    return;
                bool isOwner = room.playerDict[player.id].tempData.isOwner;
                room.playerDict[player.id].tempData.status = PlayerTempData.Statue.Lobby;
                room.playerDict.Remove(player.id);

                if (isOwner && room.playerDict.Count > 0) {
                    foreach (Player playerTemp in room.playerDict.Values)
                        playerTemp.tempData.isOwner = false;

                    Player p = room.playerDict.Values.First();
                    p.tempData.isOwner = true;
                }
            }

            lock (room.playerDict) {
                if (room.playerDict.Count == 0) {
                    lock (World.instance.roomList) {
                        World.instance.roomList.Remove(room);
                    }
                }
            }
        }

        public void BroadcastInRoom(Room room, ProtocolBase protocol) {
            lock (room.playerDict) {        //加了锁
                foreach (Player player in room.playerDict.Values) {
                    player.Send(protocol);
                }
            }
        }

        string GetGUID() {
            System.Guid guid = new Guid();
            guid = Guid.NewGuid();
            string str = guid.ToString();
            return str;
        }

        public static void BuffBoxTick(Room room) {
            Random ran = new Random();
            int buffType = ran.Next(0, room.buffBoxTypeNum);
            int posIdx = ran.Next(0, room.posIdxNum);

            string boxID = RoomSystem.instance.GetGUID();
            lock (room.boxSet) {
                room.boxSet.Add(boxID);     //J...
            }

            //广播
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("AddBuffBox");
            protocol.AddString(boxID);
            protocol.AddInt(buffType);
            protocol.AddInt(posIdx);
            RoomSystem.instance.BroadcastInRoom(room, protocol);
        }
        public static void SkillBoxTick(Room room) {
            Random ran = new Random();
            int buffType = ran.Next(0, room.skillBoxTypeNum);
            int posIdx = ran.Next(0, room.posIdxNum);

            string boxID = RoomSystem.instance.GetGUID();
            lock (room.boxSet) {
                room.boxSet.Add(boxID);
            }

            //广播
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("AddSkillBox");
            protocol.AddString(boxID);
            protocol.AddInt(buffType);
            protocol.AddInt(posIdx);
            RoomSystem.instance.BroadcastInRoom(room, protocol);
        }

        public void StartFightForRoom(Room room) {
            room.status = Room.Status.Fight;

            //生成协议
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("Fight");
            lock (room.playerDict) {
                protocol.AddInt(room.playerDict.Count);
                int teamPos1 = 1;
                int teamPos2 = 1;
                foreach (Player p in room.playerDict.Values) {
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

            //启动Box生成器
            room.buffBoxTimer.AutoReset = true;
            room.buffBoxTimer.Interval = room.buffBoxInterval;
            room.buffBoxTimer.Elapsed += new System.Timers.ElapsedEventHandler((s,e) => BuffBoxTick(room));
            room.buffBoxTimer.Start();
            room.skillBoxTimer.AutoReset = true;
            room.skillBoxTimer.Interval = room.skillBoxInterval;
            room.skillBoxTimer.Elapsed += new System.Timers.ElapsedEventHandler((s, e) => SkillBoxTick(room));
            room.skillBoxTimer.Start();

            //广播
            BroadcastInRoom(room, protocol);
        }
    }
}
