using System;

using Server.Middle;

namespace Server.Logic {

    public class HandlePlayerEvent {
        public void OnLogin(Player player) {
            Scene.instance.AddAvatar(player.id);
        }

        public void OnLogout(Player player) {
            if (player.tempData.status == PlayerTempData.Statue.Fight) {
                //Scene.instance.DelAvatar(player.id);
                Room room = player.tempData.room;
                room.ExitFight(player);
            }
            if (player.tempData.status == PlayerTempData.Statue.Room) {
                Room room = player.tempData.room;
                room.DelPlayer(player);
                if (room != null)        //没必要吧...........
                    room.Broadcast(room.GetRoomInfoBack());
            }

        }
    }
}
