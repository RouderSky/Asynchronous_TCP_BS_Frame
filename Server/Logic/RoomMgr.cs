using System;
using System.Collections.Generic;
using System.Linq;

using Common;
using Server.Core;
using Server.Middle;

//J................ 单例中的容器要十分注意线程安全
namespace Server.Logic {
    //要求单例......................
    public class RoomMgr {
        public static RoomMgr instance;
        public RoomMgr() {
            instance = this;
        }

        public List<Room> roomList = new List<Room>();

        public void CreateRoom(Player player) {
            Room room = new Room();
            lock (roomList) {
                roomList.Add(room);
                player.tempData.isOwner = true;
                room.AddPlayer(player);
            }
        }

        public ProtocolBase GetRoomListBack() {
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetRoomList");
            protocol.AddInt(roomList.Count);
            for (int i = 0; i < roomList.Count; ++i) {
                Room room = roomList[i];
                protocol.AddInt(room.playerList.Count);
                protocol.AddInt((int)room.status);
            }
            return protocol;
        }
    }
}
