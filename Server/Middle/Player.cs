using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Logic;

namespace Server.Middle {
    public class Player {
        public string id;
        //public Conn conn;		//Player中竟然也有Conn对象的引用
        public PlayerData data;
    }
}
