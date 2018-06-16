using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;	            //记得引用
using MySql.Data.MySqlClient;	//什么作用？
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;		//什么作用？

using Server.Logic;
using Server.Middle;

namespace Server.Core {
    //要求单例...
    public class DataMgr {
        MySqlConnection sqlConn;

        public static DataMgr instance;
        public DataMgr() {
            instance = this;
            Connect();
        }

        public void Connect() {
            string connStr = "DataSource=127.0.0.1;port=3306;Database=game;User Id=wanghongtao;Password=as1995222";
            sqlConn = new MySqlConnection(connStr);
            try {
                sqlConn.Open();
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]Connect " + e.Message);
                return;
            }
        }

        //防止sql注入
        //在执行用户名或者密码相关的数据库操作时，都要检查
        public bool IsSafeStr(string str) {
            bool isSafe = !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\{|\}|%|@|\*|!|\`]");     //有点看不懂这个字符串
            if (!isSafe) {
                Console.WriteLine("[DataMgr]IsSafeStr 使用非法字符");
            }
            return isSafe;
        }

        //检查数据库中是否已经有同样的id
        private bool CanRegister(string id) {
            if (!IsSafeStr(id))
                return false;

            string cmdStr = string.Format("select * from user where id ='{0}';", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return !hasRows;
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]CanRegister fail" + e.Message);
                return false;
            }
        }

        public bool Register(string id, string pw) {
            if (!IsSafeStr(id) || !IsSafeStr(pw)) {
                return false;
            }
            if (!CanRegister(id)) {
                Console.WriteLine("[DataMgr]Register 用户名已存在");
                return false;
            }

            //密码可以用md5加密再保存

            string cmdStr = string.Format("insert into user set id='{0}',pw='{1}';", id, pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]Register " + e.Message);
                return false;
            }
        }

        public bool CreatePlayer(string id){
            if(!IsSafeStr(id))
                return false;

            IFormatter formatter = new BinaryFormatter();       //数据字节化格式器
            MemoryStream stream = new MemoryStream();           //内存字节流
    
            PlayerData playerData = new PlayerData();
            try {
                formatter.Serialize(stream, playerData);
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]CreatePlayer 序列化到内存字节流失败 " + e.Message);
                return false;
            }
            byte[] byteArr = stream.ToArray();

            string cmdStr = string.Format("insert into player set id='{0}',data=@data;", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            cmd.Parameters.Add("@data", MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;
            try {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]CreatePlayer 角色数据写入数据库失败 " + e.Message);
                return false;
            }
        }

        public bool CheckPassWord(string id, string pw) {
            if (!IsSafeStr(id) || !IsSafeStr(pw))
                return false;
            string cmdStr = string.Format("select * from user where id='{0}' and pw='{1}';", id, pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]CheckPassWord " + e.Message);
                return false;
            }
        }

        
        public PlayerData GetPlayerData(string id) {
            PlayerData playerData = null;
            if (!IsSafeStr(id))
                return playerData;

            string cmdStr = string.Format("select * from player where id='{0}'", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            byte[] buffer = new byte[1];
            //从数据库读取二进制大对象  
            try {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if (!dataReader.HasRows) {
                    dataReader.Close();
                    return playerData;
                }
                dataReader.Read();

                //GetBytes：以字节的格式读取数据
                //参数一：读取哪个字段，第一个字段是0，第二个字段是1，......
                //参数二：从当前选中字段的第几个字节开始读取
                //参数三：写入到指定缓冲区
                //参数四：从指定缓冲区的第几个字节开始写入
                //参数五：最大读取字节数，G
                //返回值：指定字段的字节数，G
                long len = dataReader.GetBytes(1, 0, null, 0, 0);   //获得指定字段的字节数
                buffer = new byte[len];
                dataReader.GetBytes(1, 0, buffer, 0, (int)len);
                dataReader.Close();
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]GetPlayerData 查询失败 " + e.Message);
                return playerData;
            }

            //反序列化
            MemoryStream stream = new MemoryStream(buffer);
            try {
                BinaryFormatter formatter = new BinaryFormatter();
                playerData = (PlayerData)formatter.Deserialize(stream);
                return playerData;
            }
            catch(Exception e) {
                Console.WriteLine("[DataMgr]GetPlayerData 反序列化失败 " + e.Message);
                return playerData;
            }
        }

        public bool SavePlayer(Player player) {
            string id = player.id;
            if (!IsSafeStr(id)) {
                return false;
            }
            PlayerData playerData = player.data;

            //序列化字节数组
            MemoryStream stream = new MemoryStream();
            try {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, playerData);
            }
            catch (Exception e) {
                Console.WriteLine("[DataMgr]SavePlayer 序列化失败 " + e.Message);
                return false;
            }
            byte[] byteArr = stream.ToArray();

            //更新数据库
            string cmdStr = string.Format("update player set data=@data where id='{0}';", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            cmd.Parameters.Add("@data", MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;
            try {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e){
                Console.WriteLine("[DataMgr]SavePlayer 写入数据库失败 " + e.Message);
                return false;
            }
        }
    }
}
