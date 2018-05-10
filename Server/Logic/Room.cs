using System;
using System.Collections.Generic;
using System.Linq;

using Server.Core;
using Server.Middle;
using Server.Logic;
using Common;

namespace Server.Logic {
    public class Room {
        public enum Status {
            Prepare = 1,
            Fight = 2,
        }
        public Status status = Status.Prepare;

        //注意线程安全
        public int maxPlayers = 6;
        public Dictionary<string, Player> playerDict = new Dictionary<string, Player>();      //字典有序的吗？是不是先Add在遍历Value的时候先被遍历到？？？

        public HashSet<string> boxSet = new HashSet<string>();      //J...
        public int posIdxNum = 286;
        //buff盒子
        public int buffBoxTypeNum = 2;

        public System.Timers.Timer buffBoxTimer = new System.Timers.Timer();
        public int buffBoxInterval = 8000;
        //skill盒子
        public int skillBoxTypeNum = 13;
        public System.Timers.Timer skillBoxTimer = new System.Timers.Timer();
        public int skillBoxInterval = 8000;

        public ProtocolBase GetRoomInfoBack() {
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetRoomInfo");
            protocol.AddInt(playerDict.Count);
            foreach (Player p in playerDict.Values) {
                protocol.AddString(p.id);
                protocol.AddInt(p.tempData.team);
                protocol.AddInt(p.data.maxScore);
                protocol.AddInt(p.tempData.isOwner ? 1 : 0);
            }
            return protocol;
        }

        public bool CanStartFight() {
            if (status != Status.Prepare)
                return false;

            int count1 = 0;
            int count2 = 0;

            foreach (Player player in playerDict.Values) {
                if (player.tempData.team == 1)
                    count1++;
                if (player.tempData.team == 2)
                    count2++;
            }

            if (count1 < 1 || count2 < 1)
                return false;

            return true;
        }

        public int IsWin() {
            if (status != Status.Fight)
                return 0;

            int count1 = 0;
            int count2 = 0;
            foreach (Player player in playerDict.Values) {
                PlayerTempData pt = player.tempData;
                if (pt.team == 1 && pt.hp > 0) 
                    count1++;
                if (pt.team == 2 && pt.hp > 0)
                    count2++;
            }

            if (count1 == 0)
                return 2;
            if (count2 == 0)
                return 1;
            return 0;
        }
    }
}
