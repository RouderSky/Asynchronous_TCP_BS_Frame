using System;

using Server.Middle;
using Common;
using Server.Core;

namespace Server.Logic {

    public class HandlePlayerEvent {
        public void OnLogin(Player player) {
            
        }

        public void OnLogout(Player player) {
            if (player.tempData.status == PlayerTempData.Statue.Fight) {
                Room room = player.tempData.room;

                RoomSystem.instance.DelPlayerForRoom(room, player);

                //广播
                ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
                protocol.AddString("Hit");
                protocol.AddString(player.id);
                protocol.AddString(player.id);
                protocol.AddFloat(999);
                RoomSystem.instance.BroadcastInRoom(room, protocol);

                RoomSystem.instance.DealWithRoomWin(room);
            }
            if (player.tempData.status == PlayerTempData.Statue.Room) {
                Room room = player.tempData.room;
                RoomSystem.instance.DelPlayerForRoom(room, player);
                RoomSystem.instance.BroadcastInRoom(room, room.GetRoomInfoBack());
            }

        }
    }
}
