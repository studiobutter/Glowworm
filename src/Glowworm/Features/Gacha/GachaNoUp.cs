using Glowworm.Core;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Core.Gacha.ZZZ;
using System;
using System.Collections.Generic;

namespace Glowworm.Features.Gacha;




public class GachaNoUp
{

    public GameBiz Game { get; set; }

    public int GachaType { get; set; }

    public Dictionary<int, GachaNoUpItem> Items { get; set; } = new();



    public static Dictionary<string, GachaNoUp> Dictionary { get; } = new();



    static GachaNoUp()
    {
        AddGachaNoUpGenshin();
        AddGachaNoUpStarRail();
        AddGachaNoUpZZZ();
    }



    private static void AddGachaNoUpGenshin()
    {
        GachaNoUp hk4e301 = new GachaNoUp { Game = GameBiz.hk4e, GachaType = GenshinGachaType.CharacterEventWish };
        hk4e301.Items.Add(10000003, new GachaNoUpItem
        {
            Id = 10000003,
            Name = "?",
            NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000016, new GachaNoUpItem
        {
            Id = 10000016,
            Name = "???",
            NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000035, new GachaNoUpItem
        {
            Id = 10000035,
            Name = "??",
            NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000041, new GachaNoUpItem
        {
            Id = 10000041,
            Name = "??",
            NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000042, new GachaNoUpItem
        {
            Id = 10000042,
            Name = "??",
            NoUpTimes =
            [
                (new DateTime(2020, 9, 1), new DateTime(2021, 2, 17, 17, 59, 59)),
                (new DateTime(2021, 3, 2, 16, 00, 00), DateTime.MaxValue),
            ],
        });
        hk4e301.Items.Add(10000069, new GachaNoUpItem
        {
            Id = 10000069,
            Name = "???",
            NoUpTimes = [(new DateTime(2022, 9, 27, 18, 00, 00), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000079, new GachaNoUpItem
        {
            Id = 10000079,
            Name = "???",
            NoUpTimes = [(new DateTime(2023, 4, 11, 18, 00, 00), DateTime.MaxValue)],
        });
        hk4e301.Items.Add(10000109, new GachaNoUpItem
        {
            Id = 10000109,
            Name = "?????",
            NoUpTimes = [(new DateTime(2025, 3, 25, 18, 00, 00), DateTime.MaxValue)],
        });
        Dictionary.Add("hk4e301", hk4e301);
    }


    private static void AddGachaNoUpStarRail()
    {
        GachaNoUp hkrpg11 = new GachaNoUp { Game = GameBiz.hkrpg, GachaType = StarRailGachaType.CharacterEventWarp };
        hkrpg11.Items.Add(1003, new GachaNoUpItem
        {
            Id = 1003,
            Name = "??",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1004, new GachaNoUpItem
        {
            Id = 1004,
            Name = "???",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1101, new GachaNoUpItem
        {
            Id = 1101,
            Name = "????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1104, new GachaNoUpItem
        {
            Id = 1104,
            Name = "???",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1107, new GachaNoUpItem
        {
            Id = 1107,
            Name = "???",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1209, new GachaNoUpItem
        {
            Id = 1209,
            Name = "??",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1211, new GachaNoUpItem
        {
            Id = 1211,
            Name = "??",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        // 3.2??,????UP????
        hkrpg11.Items.Add(1102, new GachaNoUpItem
        {
            Id = 1102,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 4, 8, 18, 00, 00), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1205, new GachaNoUpItem
        {
            Id = 1205,
            Name = "?",
            NoUpTimes =
            [
                (new DateTime(2025, 4, 8, 18, 00, 00), new DateTime(2025, 7, 23, 11, 59, 59)),
                (new DateTime(2025, 8, 12, 15, 00, 00), DateTime.MaxValue),
            ],
        });
        hkrpg11.Items.Add(1208, new GachaNoUpItem
        {
            Id = 1208,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 4, 8, 18, 00, 00), DateTime.MaxValue)],
        });
        // 4.2??,????UP????
        hkrpg11.Items.Add(1006, new GachaNoUpItem
        {
            Id = 1006,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1221, new GachaNoUpItem
        {
            Id = 1221,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        hkrpg11.Items.Add(1302, new GachaNoUpItem
        {
            Id = 1302,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        Dictionary.Add("hkrpg11", hkrpg11);

        GachaNoUp hkrpg12 = new GachaNoUp { Game = GameBiz.hkrpg, GachaType = StarRailGachaType.LightConeEventWarp };
        hkrpg12.Items.Add(23000, new GachaNoUpItem
        {
            Id = 23000,
            Name = "??????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23002, new GachaNoUpItem
        {
            Id = 23002,
            Name = "???????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23003, new GachaNoUpItem
        {
            Id = 23003,
            Name = "???????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23004, new GachaNoUpItem
        {
            Id = 23004,
            Name = "?????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23005, new GachaNoUpItem
        {
            Id = 23005,
            Name = "?????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23012, new GachaNoUpItem
        {
            Id = 23012,
            Name = "????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        hkrpg12.Items.Add(23013, new GachaNoUpItem
        {
            Id = 23013,
            Name = "????",
            NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
        });
        Dictionary.Add("hkrpg12", hkrpg12);

        GachaNoUp hkrpg21 = new GachaNoUp { Game = GameBiz.hkrpg, GachaType = StarRailGachaType.CharacterCollaborationWarp };
        hkrpg21.Items.Add(1003, new GachaNoUpItem
        {
            Id = 1003,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1004, new GachaNoUpItem
        {
            Id = 1004,
            Name = "???",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1101, new GachaNoUpItem
        {
            Id = 1101,
            Name = "????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1104, new GachaNoUpItem
        {
            Id = 1104,
            Name = "???",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1107, new GachaNoUpItem
        {
            Id = 1107,
            Name = "???",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1209, new GachaNoUpItem
        {
            Id = 1209,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1211, new GachaNoUpItem
        {
            Id = 1211,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        // 3.2??,????UP????
        hkrpg21.Items.Add(1102, new GachaNoUpItem
        {
            Id = 1102,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1205, new GachaNoUpItem
        {
            Id = 1205,
            Name = "?",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1208, new GachaNoUpItem
        {
            Id = 1208,
            Name = "??",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        // 4.2??,????UP????
        hkrpg21.Items.Add(1006, new GachaNoUpItem
        {
            Id = 1006,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1221, new GachaNoUpItem
        {
            Id = 1221,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        hkrpg21.Items.Add(1302, new GachaNoUpItem
        {
            Id = 1302,
            Name = "??",
            NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
        });
        Dictionary.Add("hkrpg21", hkrpg21);

        GachaNoUp hkrpg22 = new GachaNoUp { Game = GameBiz.hkrpg, GachaType = StarRailGachaType.LightConeCollaborationWarp };
        hkrpg22.Items.Add(23000, new GachaNoUpItem
        {
            Id = 23000,
            Name = "??????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23002, new GachaNoUpItem
        {
            Id = 23002,
            Name = "???????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23003, new GachaNoUpItem
        {
            Id = 23003,
            Name = "???????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23004, new GachaNoUpItem
        {
            Id = 23004,
            Name = "?????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23005, new GachaNoUpItem
        {
            Id = 23005,
            Name = "?????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23012, new GachaNoUpItem
        {
            Id = 23012,
            Name = "????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        hkrpg22.Items.Add(23013, new GachaNoUpItem
        {
            Id = 23013,
            Name = "????",
            NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
        });
        Dictionary.Add("hkrpg22", hkrpg22);
    }


    private static void AddGachaNoUpZZZ()
    {
        GachaNoUp nap2 = new GachaNoUp { Game = GameBiz.nap, GachaType = ZZZGachaType.ExclusiveChannel };
        nap2.Items.Add(1021, new GachaNoUpItem
        {
            Id = 1021,
            Name = "??",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap2.Items.Add(1041, new GachaNoUpItem
        {
            Id = 1041,
            Name = "?11??",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap2.Items.Add(1101, new GachaNoUpItem
        {
            Id = 1101,
            Name = "???",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap2.Items.Add(1141, new GachaNoUpItem
        {
            Id = 1141,
            Name = "???",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap2.Items.Add(1181, new GachaNoUpItem
        {
            Id = 1181,
            Name = "???",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap2.Items.Add(1211, new GachaNoUpItem
        {
            Id = 1211,
            Name = "??",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        Dictionary.Add("nap2", nap2);
        Dictionary.Add("nap102", nap2);

        GachaNoUp nap3 = new GachaNoUp { Game = GameBiz.nap, GachaType = ZZZGachaType.WEngineChannel };
        nap3.Items.Add(14102, new GachaNoUpItem
        {
            Id = 14102,
            Name = "????",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap3.Items.Add(14104, new GachaNoUpItem
        {
            Id = 14104,
            Name = "???",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap3.Items.Add(14110, new GachaNoUpItem
        {
            Id = 14110,
            Name = "????",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap3.Items.Add(14114, new GachaNoUpItem
        {
            Id = 14114,
            Name = "???",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap3.Items.Add(14118, new GachaNoUpItem
        {
            Id = 14118,
            Name = "?????",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        nap3.Items.Add(14121, new GachaNoUpItem
        {
            Id = 14121,
            Name = "????",
            NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
        });
        Dictionary.Add("nap3", nap3);
        Dictionary.Add("nap103", nap3);
    }


}



public class GachaNoUpItem
{

    public int Id { get; set; }

    public string Name { get; set; }

    public List<(DateTime Start, DateTime End)> NoUpTimes { get; set; }

}



