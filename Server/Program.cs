using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Core;
using Server.Logic;
using Server.Middle;

namespace Server {
    class Program {
        static void Main(string[] args) {
#if false
            DataMgr dataMgr = new DataMgr();

            //注册
            bool ret = dataMgr.Register("Lpy", "123");
            if (ret)
                Console.WriteLine("注册成功");
            else
                Console.WriteLine("注册失败");

            //创建玩家
            ret = dataMgr.CreatePlayer("Lpy");
            if(ret)
                Console.WriteLine("创建玩家成功");
            else
                Console.WriteLine("创建玩家失败");

            //登录
            ret = dataMgr.CheckPassWord("Lpy","123");
            if(ret)
                Console.WriteLine("登录成功");
            else
                Console.WriteLine("登录失败");

            //获取玩家数据
            PlayerData pd = dataMgr.GetPlayerData("Lpy");
            if (pd != null)
                Console.WriteLine("获取玩家数据成功 " + pd.score);
            else
                Console.WriteLine("获取玩家数据失败");

            //更改玩家数据
            pd.score += 10;

            //保存玩家数据
            Player p = new Player();
            p.id = "Lpy";
            p.data = pd;
            ret = dataMgr.SavePlayer(p);
            if (ret)
                Console.WriteLine("保存玩家数据成功");
            else
                Console.WriteLine("保存玩家数据失败");

            //重新读取数据
            pd = dataMgr.GetPlayerData("Lpy");
            if (pd != null)
                Console.WriteLine("读取玩家数据成功 " + pd.score);
            else
                Console.WriteLine("读取玩家数据失败");

            Console.Read();
#else
            ServNet servNet = new ServNet();
            servNet.Start("127.0.0.1", 1234);
            Console.ReadLine();
#endif
        }
    }
}
