using System.Collections;
using System.Collections.Generic;
using System;

using Common;

namespace Client.Core {
    public class MsgDistribution {
        private int num = 15;                                       //单次处理协议的最大条数
        List<ProtocolBase> msgList = new List<ProtocolBase>();      //所有接收到的协议

        //事件监听
        public delegate void Delegate(ProtocolBase proto);
        private Dictionary<string, Delegate> eventDict = new Dictionary<string, Delegate>();    //永久监听
        private Dictionary<string, Delegate> onceDict = new Dictionary<string, Delegate>();     //单词监听

        public void AddMsg(ProtocolBase proto) {
            lock (msgList)
                msgList.Add(proto);
        }

        public void AddListener(string name, Delegate cb) {
            if (eventDict.ContainsKey(name))
                eventDict[name] += cb;
            else
                eventDict[name] = cb;
        }
        public void DelListener(string name, Delegate cb) {
            if (eventDict.ContainsKey(name)) {
                eventDict[name] -= cb;
                if (eventDict[name] == null)
                    eventDict.Remove(name);
            }
        }

        public void AddOnceListener(string name, Delegate cb) {
            if (onceDict.ContainsKey(name))
                onceDict[name] += cb;
            else
                onceDict[name] = cb;
        }
        public void DelOnceListener(string name, Delegate cb) {
            if (onceDict.ContainsKey(name)) {
                onceDict[name] -= cb;
                if (onceDict[name] == null)
                    onceDict.Remove(name);
            }
        }

        //处理接收到的协议
        public void Update() {
            for (int i = 0; i < num; ++i) {
                if (msgList.Count > 0) {
                    DispatchMsgEvent(msgList[0]);
                    lock (msgList)
                        msgList.RemoveAt(0);
                }
                else {
                    break;
                }
            }
        }

        //处理单条协议
        public void DispatchMsgEvent(ProtocolBase protocol) {
            string protoName = protocol.GetName();
            Console.WriteLine("分发消息 " + protoName);
            if (eventDict.ContainsKey(protoName)) {
                eventDict[protoName](protocol);
            }
            if (onceDict.ContainsKey(protoName)) {
                onceDict[protoName](protocol);
                //onceDict[protoName] = null;     //没必要
                onceDict.Remove(protoName);
            }
        }
    }
}