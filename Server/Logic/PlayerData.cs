using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logic {
    //如果这个类中途被修改过，为什么还能正常反序列化？？？
    [Serializable]
    public class PlayerData {
        public int maxScore = 0;
        public PlayerData() {
            //maxScore = 100;        //原本有的  
        }
    }
}
