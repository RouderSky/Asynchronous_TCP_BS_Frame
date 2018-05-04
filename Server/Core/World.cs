using System;
using System.Collections.Generic;

using Server.Logic;

namespace Server.Core {
    public class World {
        //下面都是单个Component就可以组成一个Entity，这些池可以直接放在System中
        #region Entity池
        //Component:Room
        //System:RoomSystem
        public List<Room> roomList = new List<Room>();          //使用列表是不正确的....................
        //Component:Conn
        //System:ServerSystem
        //SingletonComponent:ServNet
        public Conn[] conns;
        #endregion

        static public World instance;
        public World() {
            instance = this;
        }
    }
}
