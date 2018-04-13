using System;
using System.Collections.Generic;

using Common;
using Server.Core;
using Server.Middle;

namespace Server {
    //需要是单例............
    public class Scene {
        public static Scene instance;
        public Scene() {
            instance = this;
        }

        List<Avatar> list = new List<Avatar>();     //能不能用字典？

        public void AddAvatar(string id) {
            lock (list) {
                Avatar avatar = new Avatar();
                avatar.id = id;
                list.Add(avatar);
            }
        }

        private Avatar GetAvatar(string id) {
            for (int i = 0; i < list.Count; ++i) {
                if (list[i].id == id)
                    return list[i];
            }
            return null;
        }

        public void DelAvatar(string id) {
            lock (list) {
                Avatar avatar = GetAvatar(id);
                if (avatar != null)
                    list.Remove(avatar);
            }

            //为什么这里突然要广播下，AddAvatar和UpdateInfo都没有广播，这个设计怎么不对称的.............
            ProtocolBytes protocol = new ProtocolBytes();   //.................
            protocol.AddString("PlayerLeave");
            protocol.AddString(id);
            ServNet.instance.Broadcast(protocol);
        }

        public void UpdateInfo(string id, float x, float y, float z, int score) {
            Avatar avatar = GetAvatar(id);
            if (avatar == null)
                return;
            avatar.x = x;
            avatar.y = y;
            avatar.z = z;
            avatar.score = score;
        }

        public void SendAvatarList(Player player) {
            int count = list.Count;
            ProtocolBytes protocol = new ProtocolBytes();   //.................
            protocol.AddString("GetList");
            protocol.AddInt(count);
            for (int i = 0; i < count; ++i) {
                Avatar avatar = list[i];
                protocol.AddString(avatar.id);
                protocol.AddFloat(avatar.x);
                protocol.AddFloat(avatar.y);
                protocol.AddFloat(avatar.z);
                protocol.AddInt(avatar.score);
            }
            player.Send(protocol);
        }
    }
}
