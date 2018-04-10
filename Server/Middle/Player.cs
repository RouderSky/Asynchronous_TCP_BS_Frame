using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Logic;
using Server.Core;

namespace Server.Middle {
    public class Player {
        public string id;
        public Conn conn;		//Player中竟然也有Conn对象的引用
        public PlayerData data;
        public PlayerTempData tempData;

        public Player(string id, Conn conn) {
            this.id = id;
            this.conn = conn;
            this.tempData = new PlayerTempData();
        }

        //给当前这个玩家发送消息
        //有点冗余的函数，扰乱框架，换个名字会好点..............
        public void Send(ProtocolBase proto) {
            if (conn == null)       //有必要吗？？？
                return;
            ServNet.instance.Send(conn, proto);
        }

        public static bool KickOff(string id, ProtocolBase proto) {
            Conn[] conns = ServNet.instance.conns;
            for(int i=0;i<conns.Length;++i){
                if(conns[i] == null)        //有必要吗？
                    continue;
                if(!conns[i].isUse)
                    continue;
                if(conns[i].player == null)
                    continue;
                if(conns[i].player.id == id){
                    lock(conns[i].player){
                        if(proto != null)
                            conns[i].player.Send(proto);

                        return conns[i].player.Logout();
                    }
                }
            }
            return true;            //不应该是false？？？
        }

        public bool Logout() {
            //ServNet.instance.handlelayerEvent.OnLogout(this);

            if (!DataMgr.instance.SavePlayer(this))
                return false;

            conn.player = null;     //这句怎么单独拿出来了......
            conn.Close();

            return true;
        }
            
    }
}
