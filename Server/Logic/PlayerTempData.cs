using System;
using System.Collections.Generic;

namespace Server.Logic {
    //玩家临时数据，每次上线都不同
    //为什么不把Avatar中的数据迁移到这里来？
    public class PlayerTempData {

        public Room room;
        public int team;
        public bool isOwner = false;

        public enum Statue {
            Lobby,
            Room,
            Fight,
        }
        public Statue status;

        //删掉这个函数
        public PlayerTempData() {
            status = Statue.Lobby;
        }
    }
}
