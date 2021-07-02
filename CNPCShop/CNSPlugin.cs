﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static CNPCShop.CNSConfig;

namespace CNPCShop
{
    [ApiVersion(2, 1)]
    public class CNSPlugin : TerrariaPlugin
    {
        public override string Name => "CNPCShop";
        public override string Author => "Megghy";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Description => "自定义NPC商店出售的物品";
        public CNSPlugin(Main game) : base(game) { }
        public static List<Shop> AvilavleShops { get; set; } = new List<Shop>();
        public static CNSConfig Config { get; set; } = new CNSConfig();
        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            GeneralHooks.ReloadEvent += OnReload;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                GeneralHooks.ReloadEvent -= OnReload;
                AvilavleShops.Clear();
            }
            base.Dispose(disposing);
        }
        private void OnPostInitialize(EventArgs args) =>
            OnReload(null);
        private void OnReload(ReloadEventArgs args)
        {
            CNSConfig.Load();
            TShock.Log.ConsoleInfo("<CNPCShop> 成功读取配置文件");
        }
        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled || args.MsgID != PacketTypes.NpcTalk)
                return;

            var index = args.Msg.readBuffer[args.Index];
            var npcID = (short)(args.Msg.readBuffer[args.Index + 1]
                             + (args.Msg.readBuffer[args.Index + 2] << 8));
            if (index != args.Msg.whoAmI)
                return;
            if(npcID != -1) OnShopOpen(TShock.Players[index], npcID);
        }
        void OnShopOpen(TSPlayer plr, int npcID)
        {
            int npcType = Main.npc[npcID].type;
            if ((plr != null) && npcID != -1 && (npcID != plr.TPlayer.talkNPC))
            {
                var list = AvilavleShops.Where(s => s.NPC == npcType && (s.Groups.Contains(plr.Group.Name) || !s.Groups.Any())).ToList();
                if (list.FirstOrDefault() is { } shop)
                {
                    Task.Run(() => {
                        if(shop.OpenMessage.Any()) plr.SendMessage(shop.OpenMessage[new Random().Next(shop.OpenMessage.Count)].Replace("{name}", plr.Name), Color.White);
                        while (plr.TPlayer.talkNPC == npcID)
                        {
                            shop.RawData.ForEach(r => plr.SendRawData(r));
                            Task.Delay(100).Wait();
                        }
                        if (shop.CloseMessage.Any()) plr.SendMessage(shop.CloseMessage[new Random().Next(shop.CloseMessage.Count)].Replace("{name}", plr.Name), Color.White);
                    });
                }
            }
        }
    }
}
