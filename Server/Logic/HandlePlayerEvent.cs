using System;

using Server.Middle;

namespace Server.Logic {
    //感觉这个类里面的方法是多余的
    public class HandlePlayerEvent {
        public void OnLogin(Player player) {
            Scene.instance.AddAvatar(player.id);
        }

        public void OnLogout(Player player) {
            Scene.instance.DelAvatar(player.id);
        }
    }
}
