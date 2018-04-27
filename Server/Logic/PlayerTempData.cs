using System;
using System.Collections.Generic;

namespace Server.Logic {
    //玩家临时数据，每次上线都不同
    //为什么不把Avatar中的数据迁移到这里来？
    public class PlayerTempData {

        public enum Statue {
            Lobby,
            Room,
            Fight,
        }
        public Statue status = Statue.Lobby;

        //房间相关
        public Room room;
        public int team;
        public bool isOwner = false;

        //战场相关
        public long lastUpdatePosTime;
        public float posX;
        public float posY;
        public float posZ;
        public long lastShootTime;
        public float hp = 100;


        //删掉了构造函数
    }
}
