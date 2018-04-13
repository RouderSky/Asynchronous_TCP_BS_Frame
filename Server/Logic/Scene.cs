using System;
using System.Collections.Generic;

namespace Client.Logic {
    //需要是单例............
    public class Scene {
        public static Scene instance;
        public Scene() {
            instance = this;
        }

        List<Avatar> list = new List<Avatar>();     //能不能用字典？

        private Avatar GetAvatar(string id) {
            for (int i = 0; i < list.Count; ++i) {
                if (list[i].id == id)
                    return list[i];
            }
            return null;
        }

        public void AddPlayer(string id) {
            lock (list) {

            }
        }
    }
}
