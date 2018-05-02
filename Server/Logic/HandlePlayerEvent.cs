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

                room.DelPlayer(player);

                //广播
                ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
                protocol.AddString("Hit");
                protocol.AddString(player.id);
                protocol.AddString(player.id);
                protocol.AddFloat(999);
                room.Broadcast(protocol);

                room.UpdateWin();
            }
            if (player.tempData.status == PlayerTempData.Statue.Room) {
                Room room = player.tempData.room;
                room.DelPlayer(player);
                room.Broadcast(room.GetRoomInfoBack());
            }

        }
    }
}
