using System.Collections;
using System.Collections.Generic;

using Common;

namespace Client.Core {
    //NetMgr这个类的性质很奇怪...
    public class NetMgr {
        
        public static Connection srvConn = new Connection();
        //public static Connection platformConn = new Connection();

        //用帧循环来驱动
        public static void Update() {
            srvConn.Update();
            //platformConn.Update();
        }
    }
}
