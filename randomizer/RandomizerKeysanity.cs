using Game;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RandomizerKeysanity {

    public bool IsActive;
    private Dictionary<MoonGuid, int> doorKeyMap;
    private Dictionary<int, List<RandomizerKeysanityHintInfo>> keyClueMap;
    private Dictionary<int, string> hintMap;
    private RandomizerInventory inventory;

    public RandomizerKeysanity(RandomizerInventory inventory) {
        doorKeyMap = new Dictionary<MoonGuid, int>() {
            { new MoonGuid(-1232154268, 1164352171, -836255810, -1590216903), 300 },
            { new MoonGuid(594661726, 1329767267, -1704907880, -27301018), 301 },
            { new MoonGuid(-1932550571, 1250172391, -1917455943, -1939528727), 302 },
            { new MoonGuid(-11481148, 1140648064, 412034978, -613375286), 303 },
            { new MoonGuid(1795834119, 1281161926, -2078449998, 1300906360), 304 },
            { new MoonGuid(378303650, 1322384872, 242472089, -1590736948), 305 },
            { new MoonGuid(1873996457, 1258667073, -902258499, -2125303602), 306 },
            { new MoonGuid(1509405999, 1211109047, -376011127, -1110353735), 307 },
            { new MoonGuid(-234272614, 1227199594, 1013218476, 1564745183), 308 },
            { new MoonGuid(1974125039, 1165165912, 119656890, 137757679), 309 },
            { new MoonGuid(1472759010, 1273700124, 616551597, 280271352), 310 },
            { new MoonGuid(-1909990366, 1163800373, 1858164881, 1500718794), 311 },
        };

        hintMap = new Dictionary<int, string>() {
            { 300, "Glades Pool"},
            { 301, "Lower Spirit Caverns"},
            { 302, "Grotto"},
            { 303, "Swamp"},
            { 304, "Upper Spirit Caverns"},
            { 305, "Lower Ginso"},
            { 306, "Upper Ginso"},            
            { 307, "Misty"},
            { 308, "Forlorn"},
            { 309, "Lower Sorrow"},
            { 310, "Mid Sorrow"},
            { 311, "Upper Sorrow"},
        };

        inventory = inventory;
    }

    public void Initialize() {
        IsActive = false;
        keyClueMap = new Dictionary<int, List<RandomizerKeysanityHintInfo>>() {
            { 300, new List<RandomizerKeysanityHintInfo>()},
            { 301, new List<RandomizerKeysanityHintInfo>()},
            { 302, new List<RandomizerKeysanityHintInfo>()},
            { 303, new List<RandomizerKeysanityHintInfo>()},
            { 304, new List<RandomizerKeysanityHintInfo>()},
            { 305, new List<RandomizerKeysanityHintInfo>()},
            { 306, new List<RandomizerKeysanityHintInfo>()},
            { 307, new List<RandomizerKeysanityHintInfo>()},
            { 308, new List<RandomizerKeysanityHintInfo>()},
            { 309, new List<RandomizerKeysanityHintInfo>()},
            { 310, new List<RandomizerKeysanityHintInfo>()},
            { 311, new List<RandomizerKeysanityHintInfo>()},
        };
    }

    public void ApplyKeystoneCount(MoonGuid guid, int numberUsed) { 
        if (!IsActive) {
            return;
        }

        if (doorKeyMap.TryGetValue(guid, out var id)) {
            if(!GetDoorHint(guid)) SetDoorHint(guid);
            var count = inventory.GetRandomizerItem(id);
            Characters.Sein.Inventory.Keystones = count - numberUsed;

            if (count < countForDoor(id)) {
                Randomizer.showHint($"{hintMap[id]} ({count}/{countForDoor(id)}): {hintsForDoor(id)}");
            }
            return;
        }
        Characters.Sein.Inventory.Keystones = 0;
    }

    private bool GetDoorHint(MoonGuid guid) => 1 == (1 & (inventory.GetRandomizerItem(312) >> RandomizerLocationManager.KeystoneDoors[guid].Index));

    private void SetDoorHint(MoonGuid guid) =>  inventory.SetRandomizerItem(312, inventory.GetRandomizerItem(312) | (1 << RandomizerLocationManager.KeystoneDoors[guid].Index));

    public string MapHintForDoor(MoonGuid guid) {
        if (doorKeyMap.TryGetValue(guid, out var id)) {
            if(!IsActive) return hintMap[id];

            var count = inventory.GetRandomizerItem(id);
            if(count == countForDoor(id)) 
                return $"${hintMap[id]} {count}/{count}\n(Openable!)$";

            if(GetDoorHint(guid))
                return $"{hintMap[id]} ({count}/{countForDoor(id)})\n{hintsForDoor(id)}";

            return $"{hintMap[id]} {count}/{countForDoor(id)}\n(Touch door to get hint!)";
        }
        Randomizer.log($"Unknown door {guid}");
        return "?Unknown Door?";
    }

    public void ResetKeystoneCount() {
        if (!IsActive) {
            return;
        }

        Characters.Sein.Inventory.Keystones = 0;
    }

    private string hintsForDoor(int id) => String.Join(", ", keyClueMap[id].Where(rkhi => !Randomizer.HaveCoord(rkhi.Coords)).Select(rkhi => rkhi.Area).ToArray());

    private int countForDoor(int id) => id < 304 ? 2 : 4;

    private string GetProgress(int id, bool printKeystone) {
        if (hintMap.TryGetValue(id, out var baseHint)) {
            var canOpen = (inventory.GetRandomizerItem(id) - countForDoor(id)) == 0;
            if (canOpen) {
                return $"${baseHint}{(printKeystone ? " Keystone " : " ")}({inventory.GetRandomizerItem(id)}/{countForDoor(id)})$";
            }

            return $"{baseHint}{(printKeystone ? " Keystone " : " ")}({inventory.GetRandomizerItem(id)}/{countForDoor(id)})";
        }
        return string.Empty;
    }

    public void ShowPickupHint(int id) {
        RandomizerSwitch.PickupMessage($"{GetProgress(id, true)}");
    }

    public void ShowKeyProgress() {
        var sb = new StringBuilder();
        for (var id = 300; id < 312;) {
            sb.Append($"{GetProgress(id++, false)} {GetProgress(id++, false)} {GetProgress(id++, false)}\n");
        }
        Randomizer.printInfo(sb.ToString());
    }

    public void AddClue(int id, int coords, string area) {
        if (!keyClueMap.ContainsKey(id)) {
            return;
        }

        keyClueMap[id].Add(new RandomizerKeysanityHintInfo {
            Coords = coords,
            Area = area,
        });
    }

}