using System;
using System.Collections.Generic;

using Server.Core;

namespace Server.Logic {
    public class World {

        #region Entity池
        //Component:Room
        //System:RoomSystem
        public List<Room> roomList = new List<Room>();          //使用列表是不正确的....................
        //Component:Conn
        //System:ServNet
        public Conn[] conns;
        #endregion

        static public World instance;
        public World() {
            instance = this;
        }
    }
}
