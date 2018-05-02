using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic {
    public class World {

        //Room就像是Component，一个Room可以代表一个Entity，这个List就是World的Entity池
        public List<Room> roomList = new List<Room>();          //使用列表是不正确的....................

        static public World instance;
        public World() {
            instance = this;
        }
    }
}
