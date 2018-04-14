using System;

using Server.Middle;

namespace Server.Logic {

    public class HandlePlayerEvent {
        public void OnLogin(Player player) {
            Scene.instance.AddAvatar(player.id);
        }

        public void OnLogout(Player player) {
            Scene.instance.DelAvatar(player.id);
        }
    }
}
