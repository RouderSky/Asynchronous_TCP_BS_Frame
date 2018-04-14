using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common;
using Server.Logic;
using Server.Core;

namespace Server.Middle {
    public class Player {
        public string id;       //用户名

        private Conn conn;
        public PlayerData data;
        public PlayerTempData tempData;

        public Player(string id, Conn conn) {
            this.id = id;
            this.conn = conn;
            this.tempData = new PlayerTempData();
        }

        //给当前这个玩家发送消息
        public void Send(ProtocolBase proto) {
            //if (conn == null)       //没必要
                //return;
            ServNet.instance.Send(conn, proto);
        }

        //返回值代表，id这个用户是不是下线了
        public static bool KickOff(string id, ProtocolBase proto) {
            Conn[] conns = ServNet.instance.conns;
            for(int i=0;i<conns.Length;++i){
                //if (conns[i] == null)        //没必要
                //    continue;
                if(!conns[i].isUse)
                    continue;
                if(conns[i].player == null)
                    continue;
                if(conns[i].player.id == id){
                    lock(conns[i].player){
                        if(proto != null)
                            conns[i].player.Send(proto);
                        if (conns[i].player.Logout())
                            return true;
                        else {
                            Console.WriteLine("保存玩家数据失败，无法踢下线");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool Logout() {
            ServNet.instance.handlePlayerEvent.OnLogout(this);

            if (!DataMgr.instance.SavePlayer(this))
                return false;

            conn.player = null;
            //conn.Close();           //还是不要这样调用比较好，很恶心

            return true;
        }
            
    }
}
