using System;
using System.Collections.Generic;

using Common;
using Server.Core;
using Server.Middle;


//无用
namespace Server {
    //需要是单例............
    public class Scene {
        public static Scene instance;
        public Scene() {
            instance = this;
        }

        Dictionary<string, Avatar> dict = new Dictionary<string, Avatar>();

        //加到服务器
        public void AddAvatar(string id) {
            lock (dict) {
                Avatar avatar = new Avatar();
                avatar.id = id;
                dict[id] = avatar;
            }
        }

        //从服务器删除，广播
        public void DelAvatar(string id) {
            lock (dict) {
                dict.Remove(id);
            }

            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("AvatarLeave");
            protocol.AddString(id);
            ServNet.instance.Broadcast(protocol);
        }

        //更新到服务器，广播
        public void UpdateInfo(string id, float x, float y, float z, int score) {
            Avatar avatar = dict[id];
            if (avatar == null)
                return;
            avatar.x = x;
            avatar.y = y;
            avatar.z = z;
            avatar.score = score;

            ProtocolBase protocolRet = ServNet.instance.proto.Decode(null, 0, 0);
            protocolRet.AddString("UpdateInfo");
            protocolRet.AddString(id);
            protocolRet.AddFloat(x);
            protocolRet.AddFloat(y);
            protocolRet.AddFloat(z);
            protocolRet.AddInt(score);
            ServNet.instance.Broadcast(protocolRet);
        }

        public void SendAvatarList(Player player) {
            int count = dict.Count;
            ProtocolBase protocol = ServNet.instance.proto.Decode(null, 0, 0);
            protocol.AddString("GetList");
            protocol.AddInt(count);
            foreach (Avatar avatar in dict.Values){
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
