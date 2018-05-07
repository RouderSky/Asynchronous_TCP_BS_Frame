using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Core;
using Common;
using Server.Logic;
using Server.Middle;

namespace Server.Logic {
    //HandlePlayerMsg的扩展
    public partial class HandlePlayerMsg {

        //客户端获取房间列表信息
        //参数：无
        //返回协议:房间数，房间1内人数，房间1状态，...
        public void MsgGetRoomList(Player player, ProtocolBase protocol) {
            player.Send(RoomSystem.instance.GetRoomListBack());
        }

        //客户端请求创建房间
        //参数：无
        //返回协议:0代表成功
        public void MsgCreateRoom(Player player, ProtocolBase protocol) {
            ProtocolBase backProtocol = ServNet.instance.proto.Decode(null, 0, 0); ;
            backProtocol.AddString("CreateRoom");

            /*
            if (player.tempData.status != PlayerTempData.Statue.Lobby) {
                Console.WriteLine("玩家不在大厅，无法创建房间 " + player.id);
                backProtocol.AddInt(-1);
                player.Send(backProtocol);
                return;
            }*/

            RoomSystem.instance.CreateRoom(player);
            backProtocol.AddInt(0);
            player.Send(backProtocol);
            Console.WriteLine("创建房间成功 " + player.id);
        }

        //客户端请求加入指定房间
        //参数：房间index
        //用index来确定应该进入的房间是不准确的，因为如果中途房间被关闭了，就会进入错误的房间...
        //返回协议：0代表成功，-1代表失败，-2代表已开战，-3代表房间满
        public void MsgEnterRoom(Player player, ProtocolBase protocol) {
            int start = 0;
            string protoName = protocol.GetString(start, ref start);
            int index = protocol.GetInt(start, ref start);

            ProtocolBase backProtocol = ServNet.instance.proto.Decode(null, 0, 0);
            backProtocol.AddString("EnterRoom");

            if (index < 0 || index >= World.instance.roomList.Count) {
                Console.WriteLine("指定房间不存在，" + player.id + " 无法进入房间");
                backProtocol.AddInt(-1);
                player.Send(backProtocol);
                return;
            }

            Room room = World.instance.roomList[index];
            if(room.status != Room.Status.Prepare){
                Console.WriteLine("指定房间不是准备状态，" + player.id + " 无法进入房间");
                backProtocol.AddInt(-2);
                player.Send(backProtocol);
                return;
            }
            if(RoomSystem.instance.AddPlayerToRoom(room, player)){
                RoomSystem.instance.BroadcastInRoom(room, room.GetRoomInfoBack());
                backProtocol.AddInt(0);
                player.Send(backProtocol);
            }
            else{
                Console.WriteLine("房间已满，" + player.id + " 无法进入房间");
                backProtocol.AddInt(-3);
                player.Send(backProtocol);
            }
        }

        //客户端获取房间信息
        //参数：无
        //返回协议：人数，玩家1id，玩家1队伍，玩家1最高分，玩家1是否房主，...
        public void MsgGetRoomInfo(Player player, ProtocolBase protocol) {
            /*
            if (player.tempData.status != PlayerTempData.Statue.Room) {
                Console.WriteLine("玩家不在房间中，无法获取房间信息");
                return;
            }*/

            Room room = player.tempData.room;
            player.Send(room.GetRoomInfoBack());
        }

        //客户端请求离开房间
        //参数：无
        //返回协议：0代表成功
        public void MsgLeaveRoom(Player player, ProtocolBase protocol) {
            ProtocolBase backProtocol = ServNet.instance.proto.Decode(null, 0, 0);
            backProtocol.AddString("LeaveRoom");
            /*
            if (player.tempData.status != PlayerTempData.Statue.Room) {
                Console.WriteLine("玩家不在房间中，无需离开");
                backProtocol.AddInt(-1);
                player.Send(protocol);
                return;
            }*/
            Room room = player.tempData.room;
            RoomSystem.instance.DelPlayerForRoom(room, player);
            backProtocol.AddInt(0);
            player.Send(backProtocol);

            //广播
            //if (room != null)
                RoomSystem.instance.BroadcastInRoom(room, room.GetRoomInfoBack());
        }

    }
}
