﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string DEFAULT_ITEMS = @"
AmmoMagazine/
/Missile200mm
/NATO_25x184mm,,,,NATO_25x184mmMagazine
/NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine

Component/
/BulletproofGlass,50,2%
/Computer,30,5%,,ComputerComponent
/Construction,150,20%,,ConstructionComponent
/Detector,10,0.1%,,DetectorComponent
/Display,10,0.5%
/Explosives,5,0.1%,,ExplosivesComponent
/Girder,10,0.5%,,GirderComponent
/GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
/InteriorPlate,100,10%
/LargeTube,10,2%
/Medical,15,0.1%,,MedicalComponent
/MetalGrid,20,2%
/Motor,20,4%,,MotorComponent
/PowerCell,20,1%
/RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
/Reactor,25,2%,,ReactorComponent
/SmallTube,50,3%
/SolarCell,20,0.1%
/SteelPlate,150,40%
/Superconductor,10,1%
/Thrust,15,5%,,ThrustComponent

GasContainerObject/
/HydrogenBottle

Ingot/
/Cobalt,50,3.5%
/Gold,5,0.2%
/Iron,200,88%
/Magnesium,5,0.1%
/Nickel,30,1.5%
/Platinum,5,0.1%
/Silicon,50,2%
/Silver,20,1%
/Stone,50,2.5%
/Uranium,1,0.1%

Ore/
/Cobalt
/Gold
/Ice
/Iron
/Magnesium
/Nickel
/Platinum
/Scrap
/Silicon
/Silver
/Stone
/Uranium

OxygenContainerObject/
/OxygenBottle

PhysicalGunObject/
/AngleGrinderItem,,,,AngleGrinder
/AngleGrinder2Item,,,,AngleGrinder2
/AngleGrinder3Item,,,,AngleGrinder3
/AngleGrinder4Item,,,,AngleGrinder4
/AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
/HandDrillItem,,,,HandDrill
/HandDrill2Item,,,,HandDrill2
/HandDrill3Item,,,,HandDrill3
/HandDrill4Item,,,,HandDrill4
/PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
/RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
/UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
/WelderItem,,,,Welder
/Welder2Item,,,,Welder2
/Welder3Item,,,,Welder3
/Welder4Item,,,,Welder4
";

        // Item types which may have quantities which are not whole numbers.
        static readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> { "INGOT", "ORE" };

        // Ore subtypes which refine into Ingots with a different subtype name, or
        // which cannot be refined at all (if set to "").
        static readonly Dictionary<string, string> ORE_PRODUCT = new Dictionary<string, string> { { "ICE", "" }, { "ORGANIC", "" }, { "SCRAP", "IRON" } };

        // Block types/subtypes which restrict item types/subtypes from their first
        // inventory. Missing or "*" subtype indicates all subtypes of the given type.
        const string DEFAULT_RESTRICTIONS =
        MOB + "Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm," + NON_AMMO +
        MOB + "LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n" +
        MOB + "OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n" +
        MOB + "OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Magnesium,Ore/Organic,Ore/Platinum,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n" +
        MOB + "SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO +
        MOB + "SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm," + NON_AMMO
        ;

        /* *************
        SCRIPT INTERNALS

        Do not edit anything below unless you're sure you know what you're doing!
        */

        const int VERS_MAJ = 1, VERS_MIN = 8, VERS_REV = 0;
        const string VERS_UPD = "2019-03-11";
        const int VERSION = (VERS_MAJ * 1000000) + (VERS_MIN * 1000) + VERS_REV;

        const int MAX_CYCLE_STEPS = 11, CYCLE_LENGTH = 1;
        const bool REWRITE_TAGS = true, QUOTA_STABLE = true;
        const char TAG_OPEN = '[', TAG_CLOSE = ']';
        const string TAG_PREFIX = "TIM";
        const bool SCAN_COLLECTORS = false, SCAN_DRILLS = false, SCAN_GRINDERS = false, SCAN_WELDERS = false;
        const string MOB = "MyObjectBuilder_";
        const string NON_AMMO = "Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";
        const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
        const StringSplitOptions REE = StringSplitOptions.RemoveEmptyEntries;
        static readonly char[] SPACE = new char[] { ' ', '\t', '\u00AD' }, COLON = new char[] { ':' }, NEWLINE = new char[] { '\r', '\n' }, SPACECOMMA = new char[] { ' ', '\t', '\u00AD', ',' };
        struct Quota { public int min; public float ratio; public Quota(int m, float r) { min = m; ratio = r; } }
        struct Pair { public int a, b; public Pair(int aa, int bb) { a = aa; b = bb; } }
        struct Item { public string itype, isub; public Item(string t, string s) { itype = t; isub = s; } }
        struct Work { public Item item; public double qty; public Work(Item i, double q) { item = i; qty = q; } }

        static int lastVersion = 0;
        static string statsHeader = "";
        static string[] statsLog = new string[12];
        static long numCalls = 0;
        static double sinceLast = 0.0;
        static int numXfers, numRefs, numAsms;
        static int cycleLength = CYCLE_LENGTH, cycleStep = 0;
        static bool rewriteTags = REWRITE_TAGS;
        static char tagOpen = TAG_OPEN, tagClose = TAG_CLOSE;
        static string tagPrefix = TAG_PREFIX;
        static System.Text.RegularExpressions.Regex tagRegex = null;
        static string panelFiller = "";
        static bool foundNewItem = false;

        static Dictionary<Item, Quota> defaultQuota = new Dictionary<Item, Quota>();
        static Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>> blockSubTypeRestrictions = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>();
        static HashSet<IMyCubeGrid> dockedgrids = new HashSet<IMyCubeGrid>();
        static List<string> types = new List<string>();
        static Dictionary<string, string> typeLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> typeSubs = new Dictionary<string, List<string>>();
        static Dictionary<string, long> typeAmount = new Dictionary<string, long>();
        static List<string> subs = new List<string>();
        static Dictionary<string, string> subLabel = new Dictionary<string, string>();
        static Dictionary<string, List<string>> subTypes = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, ItemData>> typeSubData = new Dictionary<string, Dictionary<string, ItemData>>();
        static Dictionary<MyDefinitionId, Item> blueprintItem = new Dictionary<MyDefinitionId, Item>();
        static Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>> priTypeSubInvenRequest = new Dictionary<int, Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>>();
        static Dictionary<IMyTextSurface, int> qpanelPriority = new Dictionary<IMyTextSurface, int>();
        static Dictionary<IMyTextSurface, List<string>> qpanelTypes = new Dictionary<IMyTextSurface, List<string>>();
        static Dictionary<IMyTextSurface, List<string>> ipanelTypes = new Dictionary<IMyTextSurface, List<string>>();
        static List<IMyTextSurface> statusPanels = new List<IMyTextSurface>();
        static List<IMyTextSurface> debugPanels = new List<IMyTextSurface>();
        static HashSet<string> debugLogic = new HashSet<string>();
        static List<string> debugText = new List<string>();
        static Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockGtag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        static Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match> blockTag = new Dictionary<IMyTerminalBlock, System.Text.RegularExpressions.Match>();
        static HashSet<IMyInventory> invenLocked = new HashSet<IMyInventory>();
        static HashSet<IMyInventory> invenHidden = new HashSet<IMyInventory>();
        static Dictionary<IMyRefinery, HashSet<string>> refineryOres = new Dictionary<IMyRefinery, HashSet<string>>();
        static Dictionary<IMyAssembler, HashSet<Item>> assemblerItems = new Dictionary<IMyAssembler, HashSet<Item>>();
        static Dictionary<IMyFunctionalBlock, Work> producerWork = new Dictionary<IMyFunctionalBlock, Work>();
        static Dictionary<IMyFunctionalBlock, int> producerJam = new Dictionary<IMyFunctionalBlock, int>();
        static Dictionary<IMyTextSurface, Pair> panelSpan = new Dictionary<IMyTextSurface, Pair>();
        static Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>> blockErrors = new Dictionary<IMyTerminalBlock, HashSet<IMyTerminalBlock>>();


        private class ItemData
        {
            public string itype, isub, label;
            public MyDefinitionId blueprint;
            public long amount, avail, locked, quota, minimum;
            public float ratio;
            public int qpriority, hold, jam;
            public Dictionary<IMyInventory, long> invenTotal;
            public Dictionary<IMyInventory, int> invenSlot;
            public HashSet<IMyFunctionalBlock> producers;
            public Dictionary<string, double> prdSpeed;

            public static void Init(string itype, string isub, long minimum = 0L, float ratio = 0.0f, string label = "", string blueprint = "")
            {
                string itypelabel = itype, isublabel = isub;
                itype = itype.ToUpper();
                isub = isub.ToUpper();

                // new type?
                if (!typeSubs.ContainsKey(itype))
                {
                    types.Add(itype);
                    typeLabel[itype] = itypelabel;
                    typeSubs[itype] = new List<string>();
                    typeAmount[itype] = 0L;
                    typeSubData[itype] = new Dictionary<string, ItemData>();
                }

                // new subtype?
                if (!subTypes.ContainsKey(isub))
                {
                    subs.Add(isub);
                    subLabel[isub] = isublabel;
                    subTypes[isub] = new List<string>();
                }

                // new type/subtype pair?
                if (!typeSubData[itype].ContainsKey(isub))
                {
                    foundNewItem = true;
                    typeSubs[itype].Add(isub);
                    subTypes[isub].Add(itype);
                    typeSubData[itype][isub] = new ItemData(itype, isub, minimum, ratio, (label == "") ? isublabel : label, (blueprint == "") ? isublabel : blueprint);
                    if (blueprint != null)
                        blueprintItem[typeSubData[itype][isub].blueprint] = new Item(itype, isub);
                }
            } // Init()

            private ItemData(string itype, string isub, long minimum, float ratio, string label, string blueprint)
            {
                this.itype = itype;
                this.isub = isub;
                this.label = label;
                this.blueprint = (blueprint == null) ? default(MyDefinitionId) : MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprint);
                this.amount = this.avail = this.locked = this.quota = 0L;
                this.minimum = (long)((double)minimum * 1000000.0 + 0.5);
                this.ratio = (ratio / 100.0f);
                this.qpriority = -1;
                this.hold = this.jam = 0;
                this.invenTotal = new Dictionary<IMyInventory, long>();
                this.invenSlot = new Dictionary<IMyInventory, int>();
                this.producers = new HashSet<IMyFunctionalBlock>();
                this.prdSpeed = new Dictionary<string, double>();
            } // ItemData()
        } // ItemData


        /*
        * UTILITY FUNCTIONS
        */


        void InitItems(string data)
        {
            string itype = "";
            long minimum;
            float ratio;
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] words = (line.Trim() + ",,,,").Split(SPACECOMMA, 6);
                words[0] = words[0].Trim();
                if (words[0].EndsWith("/"))
                {
                    itype = words[0].Substring(0, words[0].Length - 1);
                }
                else if (itype != "" & words[0].StartsWith("/"))
                {
                    long.TryParse(words[1], out minimum);
                    float.TryParse(words[2].Substring(0, (words[2] + "%").IndexOf("%")), out ratio);
                    ItemData.Init(itype, words[0].Substring(1), minimum, ratio, words[3].Trim(), (itype == "Ingot" | itype == "Ore") ? null : words[4].Trim());
                }
            }
        } // InitItems()


        void InitBlockRestrictions(string data)
        {
            foreach (string line in data.Split(NEWLINE, REE))
            {
                string[] blockitems = (line + ":").Split(':');
                string[] block = (blockitems[0] + "/*").Split('/');
                foreach (string item in blockitems[1].Split(','))
                {
                    string[] typesub = item.ToUpper().Split('/');
                    AddBlockRestriction(block[0].Trim(SPACE), block[1].Trim(SPACE), typesub[0], ((typesub.Length > 1) ? typesub[1] : null), true);
                }
            }
        } // InitBlockRestrictions()


        void AddBlockRestriction(string btype, string bsub, string itype, string isub, bool init = false)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (!blockSubTypeRestrictions.TryGetValue(btype.ToUpper(), out bsubItypeRestr))
                blockSubTypeRestrictions[btype.ToUpper()] = bsubItypeRestr = new Dictionary<string, Dictionary<string, HashSet<string>>> { { "*", new Dictionary<string, HashSet<string>>() } };
            if (!bsubItypeRestr.TryGetValue(bsub.ToUpper(), out itypeRestr))
            {
                bsubItypeRestr[bsub.ToUpper()] = itypeRestr = new Dictionary<string, HashSet<string>>();
                if (bsub != "*" & !init)
                {
                    foreach (KeyValuePair<string, HashSet<string>> pair in bsubItypeRestr["*"])
                        itypeRestr[pair.Key] = ((pair.Value != null) ? (new HashSet<string>(pair.Value)) : null);
                }
            }
            if (isub == null | isub == "*")
            {
                itypeRestr[itype] = null;
            }
            else
            {
                (itypeRestr.TryGetValue(itype, out restr) ? restr : (itypeRestr[itype] = new HashSet<string>())).Add(isub);
            }
            if (!init) debugText.Add(btype + "/" + bsub + " does not accept " + typeLabel[itype] + "/" + subLabel[isub]);
        } // AddBlockRestriction()


        bool BlockAcceptsTypeSub(IMyCubeBlock block, string itype, string isub)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    return !(restr == null || restr.Contains(isub));
            }
            return true;
        } // BlockAcceptsTypeSub()


        HashSet<string> GetBlockAcceptedSubs(IMyCubeBlock block, string itype, HashSet<string> mysubs = null)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> bsubItypeRestr;
            Dictionary<string, HashSet<string>> itypeRestr;
            HashSet<string> restr;

            mysubs = mysubs ?? new HashSet<string>(typeSubs[itype]);
            if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr))
            {
                bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
                if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
                    mysubs.ExceptWith(restr ?? mysubs);
            }
            return mysubs;
        } // GetBlockAcceptedSubs()


        string GetBlockImpliedType(IMyCubeBlock block, string isub)
        {
            string rtype = null;
            foreach (string itype in subTypes[isub])
            {
                if (BlockAcceptsTypeSub(block, itype, isub))
                {
                    if (rtype != null)
                        return null;
                    rtype = itype;
                }
            }
            return rtype;
        } // GetBlockImpliedType()


        string GetShorthand(long amount)
        {
            long scale;
            if (amount <= 0L)
                return "0";
            if (amount < 10000L)
                return "< 0.01";
            if (amount >= 100000000000000L)
                return "" + (amount / 1000000000000L) + " M";
            scale = (long)Math.Pow(10.0, Math.Floor(Math.Log10(amount)) - 2.0);
            amount = (long)((double)amount / scale + 0.5) * scale;
            if (amount < 1000000000L)
                return (amount / 1e6).ToString("0.##");
            if (amount < 1000000000000L)
                return (amount / 1e9).ToString("0.##") + " K";
            return (amount / 1e12).ToString("0.##") + " M";
        } // GetShorthand()


        /*
        * GRID FUNCTIONS
        */


        void ScanGrids()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            IMyCubeGrid g1, g2;
            Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> gridLinks = new Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>>();
            Dictionary<IMyCubeGrid, int> gridShip = new Dictionary<IMyCubeGrid, int>();
            List<HashSet<IMyCubeGrid>> shipGrids = new List<HashSet<IMyCubeGrid>>();
            List<string> shipName = new List<string>();
            HashSet<IMyCubeGrid> grids;
            List<IMyCubeGrid> gqueue = new List<IMyCubeGrid>(); // actual Queue lacks AddRange
            int q, s1, s2;
            IMyShipConnector conn2;
            HashSet<string> tags1 = new HashSet<string>();
            HashSet<string> tags2 = new HashSet<string>();
            System.Text.RegularExpressions.Match match;
            Dictionary<int, Dictionary<int, List<string>>> shipShipDocks = new Dictionary<int, Dictionary<int, List<string>>>();
            Dictionary<int, List<string>> shipDocks;
            List<string> docks;
            HashSet<int> ships = new HashSet<int>();
            Queue<int> squeue = new Queue<int>();

            // find mechanical links
            GridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                g1 = block.CubeGrid;
                g2 = (block as IMyMechanicalConnectionBlock).TopGrid;
                if (g2 == null)
                    continue;
                (gridLinks.TryGetValue(g1, out grids) ? grids : (gridLinks[g1] = new HashSet<IMyCubeGrid>())).Add(g2);
                (gridLinks.TryGetValue(g2, out grids) ? grids : (gridLinks[g2] = new HashSet<IMyCubeGrid>())).Add(g1);
            }

            // each connected component of mechanical links is a "ship"
            foreach (IMyCubeGrid grid in gridLinks.Keys)
            {
                if (!gridShip.ContainsKey(grid))
                {
                    s1 = (grid.Max - grid.Min + Vector3I.One).Size;
                    g1 = grid;
                    gridShip[grid] = shipGrids.Count;
                    grids = new HashSet<IMyCubeGrid> { grid };
                    gqueue.Clear();
                    gqueue.AddRange(gridLinks[grid]);
                    for (q = 0; q < gqueue.Count; q++)
                    {
                        g2 = gqueue[q];
                        if (!grids.Add(g2))
                            continue;
                        s2 = (g2.Max - g2.Min + Vector3I.One).Size;
                        g1 = (s2 > s1) ? g2 : g1;
                        s1 = (s2 > s1) ? s2 : s1;
                        gridShip[g2] = shipGrids.Count;
                        gqueue.AddRange(gridLinks[g2].Except(grids));
                    }
                    shipGrids.Add(grids);
                    shipName.Add(g1.CustomName);
                }
            }

            // connectors require at least one shared dock tag, or no tags on either
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                conn2 = (block as IMyShipConnector).OtherConnector;
                if (conn2 != null && (block.EntityId < conn2.EntityId & (block as IMyShipConnector).Status == MyShipConnectorStatus.Connected))
                {
                    tags1.Clear();
                    tags2.Clear();
                    if ((match = tagRegex.Match(block.CustomName)).Success)
                    {
                        foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                tags1.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    if ((match = tagRegex.Match(conn2.CustomName)).Success)
                    {
                        foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                tags2.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    if ((tags1.Count > 0 | tags2.Count > 0) & !tags1.Overlaps(tags2))
                        continue;
                    g1 = block.CubeGrid;
                    g2 = conn2.CubeGrid;
                    if (!gridShip.TryGetValue(g1, out s1))
                    {
                        gridShip[g1] = s1 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g1 });
                        shipName.Add(g1.CustomName);
                    }
                    if (!gridShip.TryGetValue(g2, out s2))
                    {
                        gridShip[g2] = s2 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g2 });
                        shipName.Add(g2.CustomName);
                    }
                    ((shipShipDocks.TryGetValue(s1, out shipDocks) ? shipDocks : (shipShipDocks[s1] = new Dictionary<int, List<string>>())).TryGetValue(s2, out docks) ? docks : (shipShipDocks[s1][s2] = new List<string>())).Add(block.CustomName);
                    ((shipShipDocks.TryGetValue(s2, out shipDocks) ? shipDocks : (shipShipDocks[s2] = new Dictionary<int, List<string>>())).TryGetValue(s1, out docks) ? docks : (shipShipDocks[s2][s1] = new List<string>())).Add(conn2.CustomName);
                }
            }

            // starting "here", traverse all docked ships
            dockedgrids.Clear();
            dockedgrids.Add(Me.CubeGrid);
            if (!gridShip.TryGetValue(Me.CubeGrid, out s1))
                return;
            ships.Add(s1);
            dockedgrids.UnionWith(shipGrids[s1]);
            squeue.Enqueue(s1);
            while (squeue.Count > 0)
            {
                s1 = squeue.Dequeue();
                if (!shipShipDocks.TryGetValue(s1, out shipDocks))
                    continue;
                foreach (int ship2 in shipDocks.Keys)
                {
                    if (ships.Add(ship2))
                    {
                        dockedgrids.UnionWith(shipGrids[ship2]);
                        squeue.Enqueue(ship2);
                        debugText.Add(shipName[ship2] + " docked to " + shipName[s1] + " at " + String.Join(", ", shipDocks[ship2]));
                    }
                }
            }
        } // ScanGrids()


        /*
        * INVENTORY FUNCTIONS
        */


        void ScanGroups()
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;

            GridTerminalSystem.GetBlockGroups(groups);
            foreach (IMyBlockGroup group in groups)
            {
                if ((match = tagRegex.Match(group.Name)).Success)
                {
                    group.GetBlocks(blocks);
                    foreach (IMyTerminalBlock block in blocks)
                        blockGtag[block] = match;
                }
            }
        } // ScanGroups()


        void GetType(MyInventoryItem item, out string itype, out string isub)
        {
            itype = item.Type.TypeId;
            itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
            isub = item.Type.SubtypeId.ToUpper();
        }

        void FoundItem(string itype, string isub, VRage.MyFixedPoint amount, IMyInventory inv, int slot)
        {
            ItemData.Init(itype, isub, 0L, 0.0f, isub, null);

            long total = 0;
            long round = 1L;
            if(!FRACTIONAL_TYPES.Contains(isub))
            {
                round = 1000000L;
            }

            long rawAmt = amount.RawValue;
            
            debugText.Add(isub + " found " + rawAmt/round);

            typeAmount[itype] += rawAmt;

            ItemData data = typeSubData[itype][isub];
            data.amount += rawAmt;
            data.avail += rawAmt;

            data.invenTotal.TryGetValue(inv, out total);
            data.invenTotal[inv] = total + rawAmt;

            int n;
            data.invenSlot.TryGetValue(inv, out n);
            data.invenSlot[inv] = Math.Max(n, slot + 1);

            debugText.Add(" data.amount after update " + data.amount.ToString());
        }

        void ScanBlocks<T>() where T : class
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            System.Text.RegularExpressions.Match match;

            GridTerminalSystem.GetBlocksOfType<T>(blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                if (!dockedgrids.Contains(block.CubeGrid))
                    continue;

                match = tagRegex.Match(block.CustomName);
                if (match.Success)
                {
                    blockGtag.Remove(block);
                    blockTag[block] = match;
                }
                else if (blockGtag.TryGetValue(block, out match))
                {
                    blockTag[block] = match;
                }

                if ((block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                {
                    // can't sort with no conveyor port
                    invenLocked.Add(block.GetInventory(0));
                }
                else if ((block is IMyFunctionalBlock) && ((block as IMyFunctionalBlock).Enabled & block.IsFunctional))
                {
                    if ((block is IMyRefinery | block is IMyReactor | block is IMyGasGenerator) & !blockTag.ContainsKey(block))
                    {
                        // don't touch input of enabled and untagged refineries, reactors or oxygen generators
                        invenLocked.Add(block.GetInventory(0));
                    }
                    else if (block is IMyAssembler && !(block as IMyAssembler).IsQueueEmpty)
                    {
                        // don't touch input of enabled and active assemblers
                        invenLocked.Add(block.GetInventory(((block as IMyAssembler).Mode == MyAssemblerMode.Disassembly) ? 1 : 0));
                    }
                }

                if(block.HasInventory)
                {
                    
                    int numInvs = block.InventoryCount;
                    for(int i = 0; i < numInvs; ++i)
                    {
                        IMyInventory inv = block.GetInventory(i);
                        if(inv == null) { debugText.Add("Trying to access null inventory in scan blocks"); continue; }


                        List<MyInventoryItem> stacks = new List<MyInventoryItem>();
                        inv.GetItems(stacks);

                        for (int s = 0; s < stacks.Count; s++)
                        {
                            MyInventoryItem item = stacks[s];

                            string itype, isub;

                            GetType(item, out itype, out isub);
                            FoundItem(itype, isub, item.Amount, inv, s);
                        }
                   
                    }

                   

                }
            }
        } // ScanBlocks()


        void AdjustAmounts()
        {
            string itype, isub;
            List<MyInventoryItem> stacksLocked = new List<MyInventoryItem>();
            List<MyInventoryItem> stacksHidden = new List<MyInventoryItem>();

            foreach (IMyInventory inven in invenHidden)
            {
                inven.GetItems(stacksHidden, null);
                foreach (MyInventoryItem stack in stacksHidden)
                {
                    GetType(stack, out itype, out isub);
                    AdjustItemAmount(itype, isub, stack.Amount, true, true); 
                }
            }

            foreach (IMyInventory inven in invenLocked)
            {
                inven.GetItems(stacksLocked, null);
                foreach (MyInventoryItem stack in stacksLocked)
                {
                    GetType(stack, out itype, out isub);
                    AdjustItemAmount(itype, isub, stack.Amount, false, false);
                }
            }
        } // AdjustAmounts()

        void AdjustItemAmount(string itype, string isub, VRage.MyFixedPoint amount, bool available = true, bool hidden = false)
        {
            ItemData data = typeSubData[itype][isub];
            long rawAmt = amount.RawValue;

            if (!available)
            {
                data.avail -= rawAmt;
                data.locked += rawAmt;
            }

            if (hidden)
            {
                typeAmount[itype] -= rawAmt;
                typeSubData[itype][isub].amount -= rawAmt;
            }
        }


        /*
        * TAG FUNCTIONS
        */


        void ParseBlockTags()
        {
            StringBuilder name = new StringBuilder();
            IMyTextSurface blkPnl;
            IMyRefinery blkRfn;
            IMyAssembler blkAsm;
            System.Text.RegularExpressions.Match match;
            int i, priority, spanwide, spantall;
            string[] attrs, fields;
            string attr, itype, isub;
            long amount;
            float ratio;
            bool grouped, force, egg = false;

            // loop over all tagged blocks
            foreach (IMyTerminalBlock block in blockTag.Keys)
            {
                match = blockTag[block];
                attrs = match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE);
                name.Clear();
                if (!(grouped = blockGtag.ContainsKey(block)))
                {
                    name.Append(block.CustomName, 0, match.Index);
                    name.Append(tagOpen);
                    if (tagPrefix != "")
                        name.Append(tagPrefix + " ");
                }

                // loop over all tag attributes
                if ((blkPnl = (block as IMyTextSurface)) != null)
                {
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        if (lastVersion < 1005903 & (i = attr.IndexOf(":P")) > 0 & typeSubData.ContainsKey(attr.Substring(0, Math.Min(attr.Length, Math.Max(0, i)))))
                        {
                            attr = "QUOTA:" + attr;
                        }
                        else if (lastVersion < 1005903 & typeSubData.ContainsKey(attr))
                        {
                            attr = "INVEN:" + attr;
                        }
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if (attr.Length >= 4 & "STATUS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) statusPanels.Add(blkPnl);
                            name.Append("STATUS ");
                        }
                        else if (attr.Length >= 5 & "DEBUGGING".StartsWith(attr))
                        {
                            if (blkPnl.Enabled) debugPanels.Add(blkPnl);
                            name.Append("DEBUG ");
                        }
                        else if (attr == "SPAN")
                        {
                            if (fields.Length >= 3 && (int.TryParse(fields[1], out spanwide) & int.TryParse(fields[2], out spantall) & spanwide >= 1 & spantall >= 1))
                            {
                                panelSpan[blkPnl] = new Pair(spanwide, spantall);
                                name.Append("SPAN:" + spanwide + ":" + spantall + " ");
                            }
                            else
                            {
                                name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                debugText.Add("Invalid panel span rule: " + attr);
                            }
                        }
                        else if (attr == "THE")
                        {
                            egg = true;
                        }
                        else if (attr == "ENCHANTER" & egg)
                        {
                            egg = false;
                            blkPnl.SetValueFloat("FontSize", 0.2f);
                            blkPnl.WritePublicTitle("TIM the Enchanter", false);
                            blkPnl.WritePublicText(panelFiller, false);
                            blkPnl.ShowPublicTextOnScreen();
                            name.Append("THE ENCHANTER ");
                        }
                        else if (attr.Length >= 3 & "QUOTAS".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !qpanelPriority.ContainsKey(blkPnl)) qpanelPriority[blkPnl] = 0;
                            if (blkPnl.Enabled & !qpanelTypes.ContainsKey(blkPnl)) qpanelTypes[blkPnl] = new List<string>();
                            name.Append("QUOTA");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & itype != "ORE" & isub == "")
                                {
                                    if (blkPnl.Enabled) qpanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else if (fields[i].StartsWith("P") & int.TryParse(fields[i].Substring(Math.Min(1, fields[i].Length)), out priority))
                                {
                                    if (blkPnl.Enabled) qpanelPriority[blkPnl] = Math.Max(0, priority);
                                    if (priority > 0) name.Append(":P" + priority);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Invalid quota panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else if (attr.Length >= 3 & "INVENTORY".StartsWith(attr))
                        {
                            if (blkPnl.Enabled & !ipanelTypes.ContainsKey(blkPnl)) ipanelTypes[blkPnl] = new List<string>();
                            name.Append("INVEN");
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & isub == "")
                                {
                                    if (blkPnl.Enabled) ipanelTypes[blkPnl].Add(itype);
                                    name.Append(":" + typeLabel[itype]);
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Invalid inventory panel rule: " + fields[i].ToLower());
                                }
                            }
                            name.Append(" ");
                        }
                        else
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            debugText.Add("Invalid panel attribute: " + attr);
                        }
                    }
                }
                else
                {
                    blkRfn = (block as IMyRefinery);
                    blkAsm = (block as IMyAssembler);
                    foreach (string a in attrs)
                    {
                        attr = a.ToUpper();
                        if (lastVersion < 1005900 & ((blkRfn != null & attr == "ORE") | (blkAsm != null & typeSubData["COMPONENT"].ContainsKey(attr))))
                        {
                            attr = "AUTO";
                        }
                        fields = attr.Split(COLON);
                        attr = fields[0];

                        if ((attr.Length >= 4 & "LOCKED".StartsWith(attr)) | attr == "EXEMPT")
                        { // EXEMPT for AIS compat
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenLocked.Add(block.GetInventory(i));
                            name.Append(attr + " ");
                        }
                        else if (attr == "HIDDEN")
                        {
                            i = block.InventoryCount;
                            while (i-- > 0)
                                invenHidden.Add(block.GetInventory(i));
                            name.Append("HIDDEN ");
                        }
                        else if ((block is IMyShipConnector) & attr == "DOCK")
                        {
                            // handled in ScanGrids(), just rewrite
                            name.Append(String.Join(":", fields) + " ");
                        }
                        else if ((blkRfn != null | blkAsm != null) & attr == "AUTO")
                        {
                            name.Append("AUTO");
                            HashSet<string> ores, autoores = (blkRfn == null | fields.Length > 1) ? (new HashSet<string>()) : GetBlockAcceptedSubs(blkRfn, "ORE");
                            HashSet<Item> items, autoitems = new HashSet<Item>();
                            i = 0;
                            while (++i < fields.Length)
                            {
                                if (ParseItemTypeSub(null, true, fields[i], (blkRfn != null) ? "ORE" : "", out itype, out isub) & (blkRfn != null) == (itype == "ORE") & (blkRfn != null | itype != "INGOT"))
                                {
                                    if (isub == "")
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.UnionWith(typeSubs[itype]);
                                        }
                                        else
                                        {
                                            foreach (string s in typeSubs[itype])
                                                autoitems.Add(new Item(itype, s));
                                        }
                                        name.Append(":" + typeLabel[itype]);
                                    }
                                    else
                                    {
                                        if (blkRfn != null)
                                        {
                                            autoores.Add(isub);
                                        }
                                        else
                                        {
                                            autoitems.Add(new Item(itype, isub));
                                        }
                                        name.Append(":" + ((blkRfn == null & subTypes[isub].Count > 1) ? (typeLabel[itype] + "/") : "") + subLabel[isub]);
                                    }
                                }
                                else
                                {
                                    name.Append(":" + fields[i].ToLower());
                                    debugText.Add("Unrecognized or ambiguous item: " + fields[i].ToLower());
                                }
                            }
                            if (blkRfn != null)
                            {
                                if (blkRfn.Enabled)
                                    (refineryOres.TryGetValue(blkRfn, out ores) ? ores : (refineryOres[blkRfn] = new HashSet<string>())).UnionWith(autoores);
                            }
                            else
                            {
                                if (lastVersion < 1005900)
                                {
                                    blkAsm.ClearQueue();
                                    blkAsm.Repeating = false;
                                    blkAsm.Enabled = true;
                                }
                                if (blkAsm.Enabled)
                                    (assemblerItems.TryGetValue(blkAsm, out items) ? items : (assemblerItems[blkAsm] = new HashSet<Item>())).UnionWith(autoitems);
                            }
                            name.Append(" ");
                        }
                        else if (!ParseItemValueText(block, fields, "", out itype, out isub, out priority, out amount, out ratio, out force))
                        {
                            name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                            debugText.Add("Unrecognized or ambiguous item: " + attr);
                        }
                        else if (!block.HasInventory | (block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret)
                        {
                            name.Append(String.Join(":", fields).ToLower() + " ");
                            debugText.Add("Cannot sort items to " + block.CustomName + ": no conveyor-connected inventory");
                        }
                        else
                        {
                            if (isub == "")
                            {
                                foreach (string s in (force ? (IEnumerable<string>)typeSubs[itype] : (IEnumerable<string>)GetBlockAcceptedSubs(block, itype)))
                                    AddInvenRequest(block, 0, itype, s, priority, amount);
                            }
                            else
                            {
                                AddInvenRequest(block, 0, itype, isub, priority, amount);
                            }
                            if (rewriteTags & !grouped)
                            {
                                if (force)
                                {
                                    name.Append("FORCE:" + typeLabel[itype]);
                                    if (isub != "")
                                        name.Append("/" + subLabel[isub]);
                                }
                                else if (isub == "")
                                {
                                    name.Append(typeLabel[itype]);
                                }
                                else if (subTypes[isub].Count == 1 || GetBlockImpliedType(block, isub) == itype)
                                {
                                    name.Append(subLabel[isub]);
                                }
                                else
                                {
                                    name.Append(typeLabel[itype] + "/" + subLabel[isub]);
                                }
                                if (priority > 0 & priority < int.MaxValue)
                                    name.Append(":P" + priority);
                                if (amount >= 0L)
                                    name.Append(":" + (amount / 1e6));
                                name.Append(" ");
                            }
                        }
                    }
                }

                if (rewriteTags & !grouped)
                {
                    if (name[name.Length - 1] == ' ')
                        name.Length--;
                    name.Append(tagClose).Append(block.CustomName, match.Index + match.Length, block.CustomName.Length - match.Index - match.Length);
                    block.CustomName = name.ToString();
                }

                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare)
                    debugText.Add("Cannot control \"" + block.CustomName + "\" due to differing ownership");
            }
        } // ParseBlockTags()


        void ProcessQuotaPanels(bool quotaStable)
        {
            bool debug = debugLogic.Contains("quotas");
            int l, x, y, wide, size, spanx, spany, height, p, priority;
            long amount, round, total;
            float ratio;
            bool force;
            string itypeCur, itype, isub;
            string[] words, empty = new string[1] { " " };
            string[][] spanLines;
            IMyTextSurface panel2;
            IMySlimBlock slim;
            Matrix matrix = new Matrix();
            StringBuilder sb = new StringBuilder();
            List<string> qtypes = new List<string>(), errors = new List<string>(), scalesubs = new List<string>();
            Dictionary<string, SortedDictionary<string, string[]>> qtypeSubCols = new Dictionary<string, SortedDictionary<string, string[]>>();
            ItemData data;
            ScreenFormatter sf;

            // reset ore "quotas"
            foreach (ItemData d in typeSubData["ORE"].Values)
                d.minimum = (d.amount == 0L) ? 0L : Math.Max(d.minimum, d.amount);

            foreach (IMyTextSurface panel in qpanelPriority.Keys)
            {
                wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
                size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
                spanx = spany = 1;
                if (panelSpan.ContainsKey(panel))
                {
                    spanx = panelSpan[panel].a;
                    spany = panelSpan[panel].b;
                }

                // (re?)assemble (spanned?) user quota text
                spanLines = new string[spanx][];
                panel.Orientation.GetMatrix(out matrix);
                sb.Clear();
                for (y = 0; y < spany; y++)
                {
                    height = 0;
                    for (x = 0; x < spanx; x++)
                    {
                        spanLines[x] = empty;
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        panel2 = (slim != null) ? (slim.FatBlock as IMyTextSurface) : null;
                        if (panel2 != null && ("" + panel2.BlockDefinition == "" + panel.BlockDefinition & panel2.GetPublicTitle().ToUpper().Contains("QUOTAS")))
                        {
                            spanLines[x] = panel2.GetPublicText().Split('\n');
                            height = Math.Max(height, spanLines[x].Length);
                        }
                    }
                    for (l = 0; l < height; l++)
                    {
                        for (x = 0; x < spanx; x++)
                            sb.Append((l < spanLines[x].Length) ? spanLines[x][l] : " ");
                        sb.Append("\n");
                    }
                }

                // parse user quotas
                priority = qpanelPriority[panel];
                itypeCur = "";
                qtypes.Clear();
                qtypeSubCols.Clear();
                errors.Clear();
                foreach (string line in sb.ToString().Split('\n'))
                {
                    words = line.ToUpper().Split(SPACE, 4, REE);
                    if (words.Length < 1)
                    {
                    }
                    else if (ParseItemValueText(null, words, itypeCur, out itype, out isub, out p, out amount, out ratio, out force) & itype == itypeCur & itype != "" & isub != "")
                    {
                        data = typeSubData[itype][isub];
                        qtypeSubCols[itype][isub] = new string[] { data.label, "" + Math.Round(amount / 1e6, 2), "" + Math.Round(ratio * 100.0f, 2) + "%" };
                        if ((priority > 0 & (priority < data.qpriority | data.qpriority <= 0)) | (priority == 0 & data.qpriority < 0))
                        {
                            data.qpriority = priority;
                            data.minimum = amount;
                            data.ratio = ratio;
                        }
                        else if (priority == data.qpriority)
                        {
                            data.minimum = Math.Max(data.minimum, amount);
                            data.ratio = Math.Max(data.ratio, ratio);
                        }
                    }
                    else if (ParseItemValueText(null, words, "", out itype, out isub, out p, out amount, out ratio, out force) & itype != itypeCur & itype != "" & isub == "")
                    {
                        if (!qtypeSubCols.ContainsKey(itypeCur = itype))
                        {
                            qtypes.Add(itypeCur);
                            qtypeSubCols[itypeCur] = new SortedDictionary<string, string[]>();
                        }
                    }
                    else if (itypeCur != "")
                    {
                        qtypeSubCols[itypeCur][words[0]] = words;
                    }
                    else
                    {
                        errors.Add(line);
                    }
                }

                // redraw quotas
                sf = new ScreenFormatter(4, 2);
                sf.SetAlign(1, 1);
                sf.SetAlign(2, 1);
                if (qtypes.Count == 0 & qpanelTypes[panel].Count == 0)
                    qpanelTypes[panel].AddRange(types);
                foreach (string qtype in qpanelTypes[panel])
                {
                    if (!qtypeSubCols.ContainsKey(qtype))
                    {
                        qtypes.Add(qtype);
                        qtypeSubCols[qtype] = new SortedDictionary<string, string[]>();
                    }
                }
                foreach (string qtype in qtypes)
                {
                    if (qtype == "ORE")
                        continue;
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, typeLabel[qtype], true);
                    sf.Add(1, "  Min", true);
                    sf.Add(2, "  Pct", true);
                    sf.Add(3, "", true);
                    sf.AddBlankRow();
                    foreach (ItemData d in typeSubData[qtype].Values)
                    {
                        if (!qtypeSubCols[qtype].ContainsKey(d.isub))
                            qtypeSubCols[qtype][d.isub] = new string[] { d.label, "" + Math.Round(d.minimum / 1e6, 2), "" + Math.Round(d.ratio * 100.0f, 2) + "%" };
                    }
                    foreach (string qsub in qtypeSubCols[qtype].Keys)
                    {
                        words = qtypeSubCols[qtype][qsub];
                        sf.Add(0, typeSubData[qtype].ContainsKey(qsub) ? words[0] : words[0].ToLower(), true);
                        sf.Add(1, (words.Length > 1) ? words[1] : "", true);
                        sf.Add(2, (words.Length > 2) ? words[2] : "", true);
                        sf.Add(3, (words.Length > 3) ? words[3] : "", true);
                    }
                }
                WriteTableToPanel("TIM Quotas", sf, panel, true, ((errors.Count == 0) ? "" : (String.Join("\n", errors).Trim().ToLower() + "\n\n")), "");
            }

            // update effective quotas
            foreach (string qtype in types)
            {
                round = 1L;
                if (!FRACTIONAL_TYPES.Contains(qtype))
                    round = 1000000L;
                total = typeAmount[qtype];
                if (quotaStable & total > 0L)
                {
                    scalesubs.Clear();
                    foreach (ItemData d in typeSubData[qtype].Values)
                    {
                        if (d.ratio > 0.0f & total >= (long)(d.minimum / d.ratio))
                            scalesubs.Add(d.isub);
                    }
                    if (scalesubs.Count > 0)
                    {
                        scalesubs.Sort((string s1, string s2) => {
                            ItemData d1 = typeSubData[qtype][s1], d2 = typeSubData[qtype][s2];
                            long q1 = (long)(d1.amount / d1.ratio), q2 = (long)(d2.amount / d2.ratio);
                            return (q1 == q2) ? d1.ratio.CompareTo(d2.ratio) : q1.CompareTo(q2);
                        });
                        isub = scalesubs[(scalesubs.Count - 1) / 2];
                        data = typeSubData[qtype][isub];
                        total = (long)(data.amount / data.ratio + 0.5f);
                        if (debug)
                        {
                            debugText.Add("median " + typeLabel[qtype] + " is " + subLabel[isub] + ", " + (total / 1e6) + " -> " + (data.amount / 1e6 / data.ratio));
                            foreach (string qsub in scalesubs)
                            {
                                data = typeSubData[qtype][qsub];
                                debugText.Add("  " + subLabel[qsub] + " @ " + (data.amount / 1e6) + " / " + data.ratio + " => " + (long)(data.amount / 1e6 / data.ratio + 0.5f));
                            }
                        }
                    }
                }
                foreach (ItemData d in typeSubData[qtype].Values)
                {
                    amount = Math.Max(d.quota, Math.Max(d.minimum, (long)(d.ratio * total + 0.5f)));
                    d.quota = (amount / round) * round;
                }
            }
        } // ProcessQuotaPanels()


        bool ParseItemTypeSub(IMyCubeBlock block, bool force, string typesub, string qtype, out string itype, out string isub)
        {
            int t, s, found;
            string[] parts;

            itype = "";
            isub = "";
            found = 0;
            parts = typesub.Trim().Split('/');
            if (parts.Length >= 2)
            {
                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();
                if (typeSubs.ContainsKey(parts[0]) && (parts[1] == "" | typeSubData[parts[0]].ContainsKey(parts[1])))
                {
                    // exact type/subtype
                    if (force || BlockAcceptsTypeSub(block, parts[0], parts[1]))
                    {
                        found = 1;
                        itype = parts[0];
                        isub = parts[1];
                    }
                }
                else
                {
                    // type/subtype?
                    t = types.BinarySearch(parts[0]);
                    t = Math.Max(t, ~t);
                    while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                    {
                        s = typeSubs[types[t]].BinarySearch(parts[1]);
                        s = Math.Max(s, ~s);
                        while ((found < 2 & s < typeSubs[types[t]].Count) && typeSubs[types[t]][s].StartsWith(parts[1]))
                        {
                            if (force || BlockAcceptsTypeSub(block, types[t], typeSubs[types[t]][s]))
                            {
                                found++;
                                itype = types[t];
                                isub = typeSubs[types[t]][s];
                            }
                            s++;
                        }
                        // special case for gravel
                        if (found == 0 & types[t] == "INGOT" & "GRAVEL".StartsWith(parts[1]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                        {
                            found++;
                            itype = "INGOT";
                            isub = "STONE";
                        }
                        t++;
                    }
                }
            }
            else if (typeSubs.ContainsKey(parts[0]))
            {
                // exact type
                if (force || BlockAcceptsTypeSub(block, parts[0], ""))
                {
                    found++;
                    itype = parts[0];
                    isub = "";
                }
            }
            else if (subTypes.ContainsKey(parts[0]))
            {
                // exact subtype
                if (qtype != "" && typeSubData[qtype].ContainsKey(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = parts[0];
                }
                else
                {
                    t = subTypes[parts[0]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[parts[0]][t], parts[0]))
                        {
                            found++;
                            itype = subTypes[parts[0]][t];
                            isub = parts[0];
                        }
                    }
                }
            }
            else if (qtype != "")
            {
                // subtype of a known type
                s = typeSubs[qtype].BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < typeSubs[qtype].Count) && typeSubs[qtype][s].StartsWith(parts[0]))
                {
                    found++;
                    itype = qtype;
                    isub = typeSubs[qtype][s];
                    s++;
                }
                // special case for gravel
                if (found == 0 & qtype == "INGOT" & "GRAVEL".StartsWith(parts[0]))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }
            else
            {
                // type?
                t = types.BinarySearch(parts[0]);
                t = Math.Max(t, ~t);
                while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0]))
                {
                    if (force || BlockAcceptsTypeSub(block, types[t], ""))
                    {
                        found++;
                        itype = types[t];
                        isub = "";
                    }
                    t++;
                }
                // subtype?
                s = subs.BinarySearch(parts[0]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < subs.Count) && subs[s].StartsWith(parts[0]))
                {
                    t = subTypes[subs[s]].Count;
                    while (found < 2 & t-- > 0)
                    {
                        if (force || BlockAcceptsTypeSub(block, subTypes[subs[s]][t], subs[s]))
                        {
                            if (found != 1 || (itype != subTypes[subs[s]][t] | isub != "" | typeSubs[itype].Count != 1))
                                found++;
                            itype = subTypes[subs[s]][t];
                            isub = subs[s];
                        }
                    }
                    s++;
                }
                // special case for gravel
                if (found == 0 & "GRAVEL".StartsWith(parts[0]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE")))
                {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
            }

            // fill in implied subtype
            if (!force & block != null & found == 1 & isub == "")
            {
                HashSet<string> mysubs = GetBlockAcceptedSubs(block, itype);
                if (mysubs.Count == 1)
                    isub = mysubs.First();
            }

            return (found == 1);
        } // ParseItemTypeSub()


        bool ParseItemValueText(IMyCubeBlock block, string[] fields, string qtype, out string itype, out string isub, out int priority, out long amount, out float ratio, out bool force)
        {
            int f, l;
            double val, mul;

            itype = "";
            isub = "";
            priority = 0;
            amount = -1L;
            ratio = -1.0f;
            force = (block == null);

            // identify the item
            f = 0;
            if (fields[0].Trim() == "FORCE")
            {
                if (fields.Length == 1)
                    return false;
                force = true;
                f = 1;
            }
            if (!ParseItemTypeSub(block, force, fields[f], qtype, out itype, out isub))
                return false;

            // parse the remaining fields
            while (++f < fields.Length)
            {
                fields[f] = fields[f].Trim();
                l = fields[f].Length;

                if (l == 0)
                {
                }
                else if (fields[f] == "IGNORE")
                {
                    amount = 0L;
                }
                else if (fields[f] == "OVERRIDE" | fields[f] == "SPLIT")
                {
                    // these AIS tags are TIM's default behavior anyway
                }
                else if (fields[f][l - 1] == '%' & double.TryParse(fields[f].Substring(0, l - 1), out val))
                {
                    ratio = Math.Max(0.0f, (float)(val / 100.0));
                }
                else if (fields[f][0] == 'P' & double.TryParse(fields[f].Substring(1), out val))
                {
                    priority = Math.Max(1, (int)(val + 0.5));
                }
                else
                {
                    // check for numeric suffixes
                    mul = 1.0;
                    if (fields[f][l - 1] == 'K')
                    {
                        l--;
                        mul = 1e3;
                    }
                    else if (fields[f][l - 1] == 'M')
                    {
                        l--;
                        mul = 1e6;
                    }

                    // try parsing the field as an amount value
                    if (double.TryParse(fields[f].Substring(0, l), out val))
                        amount = Math.Max(0L, (long)(val * mul * 1e6 + 0.5));
                }
            }

            return true;
        } // ParseItemValueText()


        void AddInvenRequest(IMyTerminalBlock block, int inv, string itype, string isub, int priority, long amount)
        {
            long a;
            Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>> typeSubInvenReq;
            Dictionary<string, Dictionary<IMyInventory, long>> subInvenReq;
            Dictionary<IMyInventory, long> invenReq;

            // no priority -> last priority
            if (priority == 0)
                priority = int.MaxValue;

            // new priority/type/sub?
            typeSubInvenReq = (priTypeSubInvenRequest.TryGetValue(priority, out typeSubInvenReq) ? typeSubInvenReq : (priTypeSubInvenRequest[priority] = new Dictionary<string, Dictionary<string, Dictionary<IMyInventory, long>>>()));
            subInvenReq = (typeSubInvenReq.TryGetValue(itype, out subInvenReq) ? subInvenReq : (typeSubInvenReq[itype] = new Dictionary<string, Dictionary<IMyInventory, long>>()));
            invenReq = (subInvenReq.TryGetValue(isub, out invenReq) ? invenReq : (subInvenReq[isub] = new Dictionary<IMyInventory, long>()));

            // update request
            IMyInventory inven = block.GetInventory(inv);
            invenReq.TryGetValue(inven, out a);
            invenReq[inven] = amount;
            typeSubData[itype][isub].quota += Math.Min(0L, -a) + Math.Max(0L, amount);

            // disable conveyor for some block types
            // (IMyInventoryOwner is supposedly obsolete but there's no other way to do this for all of these block types at once)
            if ((block is IMyGasGenerator | block is IMyReactor | block is IMyRefinery | block is IMyUserControllableGun) & inven.Owner != null)
            {
                block.GetActionWithName("UseConveyor").Apply(block);
                debugText.Add("Disabling conveyor system for " + block.CustomName);
            }
        } // AddInvenRequest()


        /*
        * TRANSFER FUNCTIONS
        */


        void AllocateItems(bool limited)
        {
            List<int> priorities;
            
            // establish priority order, adding 0 for refinery management
            priorities = new List<int>(priTypeSubInvenRequest.Keys);
            priorities.Sort();

            foreach (int p in priorities)
            {

                
                foreach (string itype in priTypeSubInvenRequest[p].Keys)
                {

                    foreach (string isub in priTypeSubInvenRequest[p][itype].Keys)
                    {
                        AllocateItemBatch(limited, p, itype, isub);
                    }
                }
            }

            // if we just finished the unlimited requests, check for leftovers
            if (!limited)
            {
                foreach (string itype in types)
                {
                    foreach (ItemData data in typeSubData[itype].Values)
                    {
                        if (data.avail > 0L)
                            debugText.Add("No place to put " + GetShorthand(data.avail) + " " + typeLabel[itype] + "/" + subLabel[data.isub] + ", containers may be full");
                    }
                }
            }
        } // AllocateItems()


        void AllocateItemBatch(bool limited, int priority, string itype, string isub)
        {
            bool debug = debugLogic.Contains("sorting");
            int locked, dropped;
            long totalrequest, totalavail, request, avail, amount, moved, round;
            List<IMyInventory> invens = null;
            Dictionary<IMyInventory, long> invenRequest;

            if (debug) { debugText.Add("sorting " + typeLabel[itype] + "/" + subLabel[isub] + " lim=" + limited + " p=" + priority); }
            
            round = 1L;
            if (!FRACTIONAL_TYPES.Contains(itype))
                round = 1000000L;
            invenRequest = new Dictionary<IMyInventory, long>();
            ItemData data = typeSubData[itype][isub];
            
            // sum up the requests
            totalrequest = 0L;
            foreach (IMyInventory reqInven in priTypeSubInvenRequest[priority][itype][isub].Keys)
            {
                request = priTypeSubInvenRequest[priority][itype][isub][reqInven];
                if (request != 0L & limited == (request >= 0L))
                {
                    if (request < 0L)
                    {
                        request = 1000000L;
                        if (reqInven.MaxVolume != VRage.MyFixedPoint.MaxValue)
                            request = (long)((double)reqInven.MaxVolume * 1e6);
                    }
                    invenRequest[reqInven] = request;
                    totalrequest += request;
                }
            }
            if (debug) { debugText.Add("total req=" + (totalrequest / 1e6)); }

            if (totalrequest <= 0L)
                return;
            
            totalavail = data.avail + data.locked;
            if (debug) { debugText.Add("total avail=" + (totalavail / 1e6)); }

            // disqualify any locked invens which already have their share
            if (totalavail > 0L)
            {
                invens = new List<IMyInventory>(data.invenTotal.Keys);
                do
                {
                    locked = 0;
                    dropped = 0;
                    foreach (IMyInventory amtInven in invens)
                    {
                        avail = data.invenTotal[amtInven];
                        if (avail > 0L & invenLocked.Contains(amtInven))
                        {
                            locked++;
                            invenRequest.TryGetValue(amtInven, out request);
                            amount = (long)((double)request / totalrequest * totalavail);
                            if (limited)
                                amount = Math.Min(amount, request);
                            amount = (amount / round) * round;

                            if (avail >= amount)
                            {
                                if (debug) { debugText.Add("locked " + (amtInven.Owner == null ? "???" : (amtInven.Owner as IMyTerminalBlock).CustomName) + " gets " + (amount / 1e6) + ", has " + (avail / 1e6)); }
                                dropped++;
                                totalrequest -= request;
                                invenRequest[amtInven] = 0L;
                                totalavail -= avail;
                                data.locked -= avail;
                                data.invenTotal[amtInven] = 0L;
                            }
                        }
                    }
                } while (locked > dropped & dropped > 0);
            }

            // allocate the remaining available items
            foreach (IMyInventory reqInven in invenRequest.Keys)
            {
                // calculate this inven's allotment
                request = invenRequest[reqInven];
                if (request <= 0L | totalrequest <= 0L | totalavail <= 0L)
                {
                    if (limited & request > 0L) debugText.Add("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                    continue;
                }
                amount = (long)((double)request / totalrequest * totalavail);
                if (limited)
                    amount = Math.Min(amount, request);
                amount = (amount / round) * round;
                if (debug) { debugText.Add((reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName) + " gets " + (request / 1e6) + " / " + (totalrequest / 1e6) + " of " + (totalavail / 1e6) + " = " + (amount / 1e6)); }
                totalrequest -= request;

                // check how much it already has
                if (data.invenTotal.TryGetValue(reqInven, out avail))
                {
                    avail = Math.Min(avail, amount);
                    amount -= avail;
                    totalavail -= avail;
                    if (invenLocked.Contains(reqInven))
                    {
                        data.locked -= avail;
                    }
                    else
                    {
                        data.avail -= avail;
                    }
                    data.invenTotal[reqInven] -= avail;
                }

                // get the rest from other unlocked invens
                moved = 0L;
                foreach (IMyInventory amtInven in invens)
                {
                    avail = Math.Min(data.invenTotal[amtInven], amount);
                    moved = 0L;
                    if (avail > 0L & invenLocked.Contains(amtInven) == false)
                    {
                        moved = TransferItem(itype, isub, avail, amtInven, reqInven);
                        amount -= moved;
                        totalavail -= moved;
                        data.avail -= moved;
                        data.invenTotal[amtInven] -= moved;
                    }
                    // if we moved some but not all, we're probably full
                    if (amount <= 0L | (moved != 0L & moved != avail))
                        break;
                }

                if (limited & amount > 0L)
                {
                    debugText.Add("Insufficient " + typeLabel[itype] + "/" + subLabel[isub] + " to satisfy " + (reqInven.Owner == null ? "???" : (reqInven.Owner as IMyTerminalBlock).CustomName));
                    continue;
                }
            }

            if (debug) { debugText.Add("" + (totalavail / 1e6) + " left over"); }
        } // AllocateItemBatch()


        long TransferItem(string itype, string isub, long amount, IMyInventory fromInven, IMyInventory toInven)
        {
            bool debug = debugLogic.Contains("sorting");
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            int s;
            VRage.MyFixedPoint remaining, moved;
            uint id;
            string stype, ssub;

            remaining = (VRage.MyFixedPoint)(amount / 1e6);
            fromInven.GetItems(stacks,null);
            s = Math.Min(typeSubData[itype][isub].invenSlot[fromInven], stacks.Count);
            while (remaining > 0 & s-- > 0)
            {
                GetType(stacks[s], out stype, out ssub);

                if (stype == itype & ssub == isub)
                {
                    moved = stacks[s].Amount;
                    id = stacks[s].ItemId;
                    if (debug) { debugText.Add("" + remaining.ToString() + " remaining, attempting to send"); }

                    if (fromInven == toInven)
                    {
                        remaining -= moved;
                        if (remaining < 0)
                            remaining = 0;
                    }
                    else if (fromInven.TransferItemTo(toInven, stacks[s], remaining))
                    {
                        stacks.Clear();
                        fromInven.GetItems(stacks, null);
                        if (s < stacks.Count && stacks[s].ItemId == id)
                            moved -= stacks[s].Amount;
                        if (moved <= 0)
                        {
                            if ((double)toInven.CurrentVolume < (double)toInven.MaxVolume / 2 & toInven.Owner != null)
                            {
                                var/*SerializableDefinitionId*/ bdef = (toInven.Owner as IMyCubeBlock).BlockDefinition;
                                AddBlockRestriction(bdef.TypeIdString, bdef.SubtypeName, itype, isub);
                            }
                            s = 0;
                        }
                        else
                        {
                            numXfers++;
                            if (debug)
                            {
                                debugText.Add(
                                "Transferred " + GetShorthand((long)((double)moved * 1e6)) + " " + typeLabel[itype] + "/" + subLabel[isub] +
                                " from " + (fromInven.Owner == null ? "???" : (fromInven.Owner as IMyTerminalBlock).CustomName) + " to " + (toInven.Owner == null ? "???" : (toInven.Owner as IMyTerminalBlock).CustomName)
                                );
                            }
                            //					volume -= (double)fromInven.CurrentVolume;
                            //					typeSubData[itype][isub].volume = (1000.0 * volume / (double)moved);
                        }
                        remaining -= moved;
                    }
                    else if (!fromInven.IsConnectedTo(toInven) & fromInven.Owner != null & toInven.Owner != null)
                    {
                        if (!blockErrors.ContainsKey(fromInven.Owner as IMyTerminalBlock))
                            blockErrors[fromInven.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>();
                        blockErrors[fromInven.Owner as IMyTerminalBlock].Add(toInven.Owner as IMyTerminalBlock);
                        s = 0;
                    }
                }
            }

            return amount - (long)((double)remaining * 1e6 + 0.5);
        } // TransferItem()


        /*
        * MANAGEMENT FUNCTIONS
        */


        void ManageRefineries()
        {
            if (!typeSubs.ContainsKey("ORE") | !typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = debugLogic.Contains("refineries");
            string itype, itype2, isub, isub2, isubIngot;
            ItemData data;
            int level, priority;
            List<string> ores = new List<string>();
            Dictionary<string, int> oreLevel = new Dictionary<string, int>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            double speed, oldspeed;
            Work work;
            bool ready;
            List<IMyRefinery> refineries = new List<IMyRefinery>();

            if (debug) debugText.Add("Refinery management:");

            // scan inventory levels
            foreach (string isubOre in typeSubs["ORE"])
            {
                if (!ORE_PRODUCT.TryGetValue(isubOre, out isubIngot))
                    isubIngot = isubOre;
                if (isubIngot != "" & typeSubData["ORE"][isubOre].avail > 0L & typeSubData["INGOT"].TryGetValue(isubIngot, out data))
                {
                    if (data.quota > 0L)
                    {
                        level = (int)(100L * data.amount / data.quota);
                        ores.Add(isubOre);
                        oreLevel[isubOre] = level;
                        if (debug) debugText.Add("  " + subLabel[isubIngot] + " @ " + (data.amount / 1e6) + "/" + (data.quota / 1e6) + "," + ((isubOre == isubIngot) ? "" : (" Ore/" + subLabel[isubOre])) + " L=" + level + "%");
                    }
                }
            }

            // identify refineries that are ready for a new assignment
            foreach (IMyRefinery rfn in refineryOres.Keys)
            {
                itype = itype2 = isub = isub2 = "";
                rfn.GetInventory(0).GetItems(stacks,null);
                if (stacks.Count > 0)
                {
                    itype = stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToUpper();
                    if (itype == "ORE" & oreLevel.ContainsKey(isub))
                        oreLevel[isub] += Math.Max(1, oreLevel[isub] / refineryOres.Count);
                    if (stacks.Count > 1)
                    {
                        itype2 = stacks[1].Type.TypeId;
                        itype2 = itype2.Substring(itype2.LastIndexOf('_') + 1).ToUpper();
                        isub2 = stacks[1].Type.SubtypeId.ToUpper();
                        if (itype2 == "ORE" & oreLevel.ContainsKey(isub2))
                            oreLevel[isub2] += Math.Max(1, oreLevel[isub2] / refineryOres.Count);
                        AddInvenRequest(rfn, 0, itype2, isub2, -2, (long)((double)stacks[1].Amount * 1e6 + 0.5));
                    }
                }
                if (producerWork.TryGetValue(rfn, out work))
                {
                    data = typeSubData[work.item.itype][work.item.isub];
                    oldspeed = (data.prdSpeed.TryGetValue("" + rfn.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
                    speed = ((work.item.isub == isub) ? Math.Max(work.qty - (double)stacks[0].Amount, 0.0) : Math.Max(work.qty, oldspeed));
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, 0.2), 10000.0);
                    data.prdSpeed["" + rfn.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) debugText.Add("  Update " + rfn.BlockDefinition.SubtypeName + ":" + subLabel[work.item.isub] + " refine speed: " + ((int)(oldspeed + 0.5)) + " -> " + ((int)(speed + 0.5)) + "kg/cycle");
                }
                if (refineryOres[rfn].Count > 0) refineryOres[rfn].IntersectWith(oreLevel.Keys); else refineryOres[rfn].UnionWith(oreLevel.Keys);
                ready = (refineryOres[rfn].Count > 0);
                if (stacks.Count > 0)
                {
                    speed = (itype == "ORE" ? (typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0) : 1e6);
                    AddInvenRequest(rfn, 0, itype, isub, -1, (long)Math.Min((double)stacks[0].Amount * 1e6 + 0.5, 10 * speed * 1e6 + 0.5));
                    ready = (ready & itype == "ORE" & (double)stacks[0].Amount < 2.5 * speed & stacks.Count == 1);
                }
                if (ready)
                    refineries.Add(rfn);
                if (debug) debugText.Add(
                    "  " + rfn.CustomName + ((stacks.Count < 1) ? " idle" : (
                        " refining " + (int)stacks[0].Amount + "kg " + ((isub == "") ? "unknown" : (
                            subLabel[isub] + (!oreLevel.ContainsKey(isub) ? "" : (" (L=" + oreLevel[isub] + "%)"))
                        )) + ((stacks.Count < 2) ? "" : (
                            ", then " + (int)stacks[1].Amount + "kg " + ((isub2 == "") ? "unknown" : (
                                subLabel[isub2] + (!oreLevel.ContainsKey(isub2) ? "" : (" (L=" + oreLevel[isub2] + "%)"))
                            ))
                        ))
                    )) + "; " + ((oreLevel.Count == 0) ? "nothing to do" : (ready ? "ready" : ((refineryOres[rfn].Count == 0) ? "restricted" : "busy")))
                );
            }

            // skip refinery:ore assignment if there are no ores or ready refineries
            if (ores.Count > 0 & refineries.Count > 0)
            {
                ores.Sort((string o1, string o2) => {
                    string i1, i2;
                    if (!ORE_PRODUCT.TryGetValue(o1, out i1)) i1 = o1;
                    if (!ORE_PRODUCT.TryGetValue(o2, out i2)) i2 = o2;
                    return -1 * typeSubData["INGOT"][i1].quota.CompareTo(typeSubData["INGOT"][i2].quota);
                });
                refineries.Sort((IMyRefinery r1, IMyRefinery r2) => refineryOres[r1].Count.CompareTo(refineryOres[r2].Count));
                foreach (IMyRefinery rfn in refineries)
                {
                    isub = "";
                    level = int.MaxValue;
                    foreach (string isubOre in ores)
                    {
                        if ((isub == "" | oreLevel[isubOre] < level) & refineryOres[rfn].Contains(isubOre))
                        {
                            isub = isubOre;
                            level = oreLevel[isub];
                        }
                    }
                    if (isub != "")
                    {
                        numRefs++;
                        rfn.UseConveyorSystem = false;
                        priority = rfn.GetInventory(0).IsItemAt(0) ? -4 : -3;
                        speed = (typeSubData["ORE"][isub].prdSpeed.TryGetValue("" + rfn.BlockDefinition, out speed) ? speed : 1.0);
                        AddInvenRequest(rfn, 0, "ORE", isub, priority, (long)(5 * speed * 1e6 + 0.5));
                        oreLevel[isub] += Math.Min(Math.Max((int)(oreLevel[isub] * 0.41), 1), (100 / refineryOres.Count));
                        if (debug) debugText.Add("  " + rfn.CustomName + " assigned " + ((int)(5 * speed + 0.5)) + "kg " + subLabel[isub] + " (L=" + oreLevel[isub] + "%)");
                    }
                    else if (debug) debugText.Add("  " + rfn.CustomName + " unassigned, nothing to do");
                }
            }

            for (priority = -1; priority >= -4; priority--)
            {
                if (priTypeSubInvenRequest.ContainsKey(priority))
                {
                    foreach (string isubOre in priTypeSubInvenRequest[priority]["ORE"].Keys)
                        AllocateItemBatch(true, priority, "ORE", isubOre);
                }
            }
        } // ManageRefineries()


        void ManageAssemblers()
        {
            if (!typeSubs.ContainsKey("INGOT"))
                return;

            bool debug = debugLogic.Contains("assemblers");
            long ttlCmp;
            int level, amount;
            ItemData data, data2;
            Item item, item2;
            List<Item> items;
            Dictionary<Item, int> itemLevel = new Dictionary<Item, int>(), itemPar = new Dictionary<Item, int>();
            List<MyProductionItem> queue = new List<MyProductionItem>();
            double speed, oldspeed;
            Work work;
            bool ready, jam;
            List<IMyAssembler> assemblers = new List<IMyAssembler>();

            if (debug) debugText.Add("Assembler management:");

            // scan inventory levels
            typeAmount.TryGetValue("COMPONENT", out ttlCmp);
            amount = 90 + (int)(10 * typeSubData["INGOT"].Values.Min(d => (d.isub != "URANIUM" & (d.minimum > 0L | d.ratio > 0.0f)) ? (d.amount / Math.Max((double)d.minimum, 17.5 * d.ratio * ttlCmp)) : 2.0));
            if (debug) debugText.Add("  Component par L=" + amount + "%");
            foreach (string itype in types)
            {
                if (itype != "ORE" & itype != "INGOT")
                {
                    foreach (string isub in typeSubs[itype])
                    {
                        data = typeSubData[itype][isub];
                        data.hold = Math.Max(0, data.hold - 1);
                        item = new Item(itype, isub);
                        itemPar[item] = ((itype == "COMPONENT" & data.ratio > 0.0f) ? amount : 100);
                        level = (int)(100L * data.amount / Math.Max(1L, data.quota));
                        if (data.quota > 0L & level < itemPar[item] & data.blueprint != default(MyDefinitionId))
                        {
                            if (data.hold == 0) itemLevel[item] = level;
                            if (debug) debugText.Add("  " + typeLabel[itype] + "/" + subLabel[isub] + ((data.hold > 0) ? "" : (" @ " + (data.amount / 1e6) + "/" + (data.quota / 1e6) + ", L=" + level + "%")) + ((data.hold > 0 | data.jam > 0) ? ("; HOLD " + data.hold + "/" + (10 * data.jam)) : ""));
                        }
                    }
                }
            }

            // identify assemblers that are ready for a new assignment
            foreach (IMyAssembler asm in assemblerItems.Keys)
            {
                ready = jam = false;
                data = data2 = null;
                item = item2 = new Item("", "");
                if (!asm.IsQueueEmpty)
                {
                    asm.GetQueue(queue);
                    data = (blueprintItem.TryGetValue(queue[0].BlueprintId, out item) ? typeSubData[item.itype][item.isub] : null);
                    if (data != null & itemLevel.ContainsKey(item))
                        itemLevel[item] += Math.Max(1, (int)(1e8 * (double)queue[0].Amount / data.quota + 0.5));
                    if (queue.Count > 1 && (blueprintItem.TryGetValue(queue[1].BlueprintId, out item2) & itemLevel.ContainsKey(item2)))
                        itemLevel[item2] += Math.Max(1, (int)(1e8 * (double)queue[1].Amount / typeSubData[item2.itype][item2.isub].quota + 0.5));
                }
                if (producerWork.TryGetValue(asm, out work))
                {
                    data2 = typeSubData[work.item.itype][work.item.isub];
                    oldspeed = (data2.prdSpeed.TryGetValue("" + asm.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
                    if (work.item.itype != item.itype | work.item.isub != item.isub)
                    {
                        speed = Math.Max(oldspeed, (asm.IsQueueEmpty ? 2 : 1) * work.qty);
                        producerJam.Remove(asm);
                    }
                    else if (asm.IsProducing)
                    {
                        speed = work.qty - (double)queue[0].Amount + asm.CurrentProgress;
                        producerJam.Remove(asm);
                    }
                    else
                    {
                        speed = Math.Max(oldspeed, work.qty - (double)queue[0].Amount + asm.CurrentProgress);
                        if ((producerJam[asm] = (producerJam.TryGetValue(asm, out level) ? level : 0) + 1) >= 3)
                        {
                            debugText.Add("  " + asm.CustomName + " is jammed by " + subLabel[item.isub]);
                            producerJam.Remove(asm);
                            asm.ClearQueue();
                            data2.hold = 10 * ((data2.jam < 1 | data2.hold < 1) ? (data2.jam = Math.Min(10, data2.jam + 1)) : data2.jam);
                            jam = true;
                        }
                    }
                    speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, Math.Max(0.2, 0.5 * oldspeed)), Math.Min(1000.0, 2.0 * oldspeed));
                    data2.prdSpeed["" + asm.BlockDefinition] = speed;
                    if (debug & (int)(oldspeed + 0.5) != (int)(speed + 0.5)) debugText.Add("  Update " + asm.BlockDefinition.SubtypeName + ":" + typeLabel[work.item.itype] + "/" + subLabel[work.item.isub] + " assemble speed: " + ((int)(oldspeed * 100) / 100.0) + " -> " + ((int)(speed * 100) / 100.0) + "/cycle");
                }
                if (assemblerItems[asm].Count == 0) assemblerItems[asm].UnionWith(itemLevel.Keys); else assemblerItems[asm].IntersectWith(itemLevel.Keys);
                speed = ((data != null && data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed)) ? speed : 1.0);
                if (!jam & (asm.IsQueueEmpty || (((double)queue[0].Amount - asm.CurrentProgress) < 2.5 * speed & queue.Count == 1 & asm.Mode == MyAssemblerMode.Assembly)))
                {
                    if (data2 != null) data2.jam = Math.Max(0, data2.jam - ((data2.hold < 1) ? 1 : 0));
                    if (ready = (assemblerItems[asm].Count > 0)) assemblers.Add(asm);
                }
                if (debug) debugText.Add(
                    "  " + asm.CustomName + (asm.IsQueueEmpty ? " idle" : (
                        ((asm.Mode == MyAssemblerMode.Assembly) ? " making " : " breaking ") + queue[0].Amount + "x " + ((item.itype == "") ? "unknown" : (
                            subLabel[item.isub] + (!itemLevel.ContainsKey(item) ? "" : (" (L=" + itemLevel[item] + "%)"))
                        )) + ((queue.Count <= 1) ? "" : (
                            ", then " + queue[1].Amount + "x " + ((item2.itype == "") ? "unknown" : (
                                subLabel[item2.isub] + (!itemLevel.ContainsKey(item2) ? "" : (" (L=" + itemLevel[item2] + "%)"))
                            ))
                        ))
                    )) + "; " + ((itemLevel.Count == 0) ? "nothing to do" : (ready ? "ready" : ((assemblerItems[asm].Count == 0) ? "restricted" : "busy")))
                );
            }

            // skip assembler:item assignments if there are no needed items or ready assemblers
            if (itemLevel.Count > 0 & assemblers.Count > 0)
            {
                items = new List<Item>(itemLevel.Keys);
                items.Sort((i1, i2) => -1 * typeSubData[i1.itype][i1.isub].quota.CompareTo(typeSubData[i2.itype][i2.isub].quota));
                assemblers.Sort((IMyAssembler a1, IMyAssembler a2) => assemblerItems[a1].Count.CompareTo(assemblerItems[a2].Count));
                foreach (IMyAssembler asm in assemblers)
                {
                    item = new Item("", "");
                    level = int.MaxValue;
                    foreach (Item i in items)
                    {
                        if (itemLevel[i] < Math.Min(level, itemPar[i]) & assemblerItems[asm].Contains(i) & typeSubData[i.itype][i.isub].hold < 1)
                        {
                            item = i;
                            level = itemLevel[i];
                        }
                    }
                    if (item.itype != "")
                    {
                        numAsms++;
                        asm.UseConveyorSystem = true;
                        asm.CooperativeMode = false;
                        asm.Repeating = false;
                        asm.Mode = MyAssemblerMode.Assembly;
                        data = typeSubData[item.itype][item.isub];
                        speed = (data.prdSpeed.TryGetValue("" + asm.BlockDefinition, out speed) ? speed : 1.0);
                        amount = Math.Max((int)(5 * speed), 1);
                        asm.AddQueueItem(data.blueprint, (double)amount);
                        itemLevel[item] += (int)Math.Ceiling(1e8 * (double)amount / data.quota);
                        if (debug) debugText.Add("  " + asm.CustomName + " assigned " + amount + "x " + subLabel[item.isub] + " (L=" + itemLevel[item] + "%)");
                    }
                    else if (debug) debugText.Add("  " + asm.CustomName + " unassigned, nothing to do");
                }
            }
        } // ManageAssemblers()


        /*
        * PANEL DISPLAYS
        */


        void ScanProduction()
        {
            List<IMyTerminalBlock> blocks1 = new List<IMyTerminalBlock>(), blocks2 = new List<IMyTerminalBlock>();
            List<MyInventoryItem> stacks = new List<MyInventoryItem>();
            string itype, isub, isubIng;
            List<MyProductionItem> queue = new List<MyProductionItem>();
            Item item;

            producerWork.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks2, blk => dockedgrids.Contains(blk.CubeGrid));
            foreach (IMyFunctionalBlock blk in blocks1.Concat(blocks2))
            {

                blk.GetInventory(0).GetItems(stacks,null);
                if (stacks.Count > 0 & blk.Enabled)
                {
                    itype = stacks[0].Type.TypeId;
                    itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
                    isub = stacks[0].Type.SubtypeId.ToUpper();
                    if (typeSubs.ContainsKey(itype) & subTypes.ContainsKey(isub))
                        typeSubData[itype][isub].producers.Add(blk);
                    if (itype == "ORE" & (ORE_PRODUCT.TryGetValue(isub, out isubIng) ? isubIng : (isubIng = isub)) != "" & typeSubData["INGOT"].ContainsKey(isubIng))
                        typeSubData["INGOT"][isubIng].producers.Add(blk);
                    producerWork[blk] = new Work(new Item(itype, isub), (double)stacks[0].Amount);
                }
            }

            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
            foreach (IMyAssembler blk in blocks1)
            {
                if (blk.Enabled & !blk.IsQueueEmpty & blk.Mode == MyAssemblerMode.Assembly)
                {
                    blk.GetQueue(queue);
                    if (blueprintItem.TryGetValue(queue[0].BlueprintId, out item))
                    {
                        if (typeSubs.ContainsKey(item.itype) & subTypes.ContainsKey(item.isub))
                            typeSubData[item.itype][item.isub].producers.Add(blk);
                        producerWork[blk] = new Work(item, (double)queue[0].Amount - blk.CurrentProgress);
                    }
                }
            }
        } // ScanProduction()


        void UpdateInventoryPanels()
        {
            string text, header2, header5;
            Dictionary<string, List<IMyTextSurface>> itypesPanels = new Dictionary<string, List<IMyTextSurface>>();
            ScreenFormatter sf;
            long maxamt, maxqta;

            foreach (IMyTextSurface panel in ipanelTypes.Keys)
            {
                text = String.Join("/", ipanelTypes[panel]);
                if (itypesPanels.ContainsKey(text)) itypesPanels[text].Add(panel); else itypesPanels[text] = new List<IMyTextSurface>() { panel };
            }
            foreach (List<IMyTextSurface> panels in itypesPanels.Values)
            {
                sf = new ScreenFormatter(6);
                sf.SetBar(0);
                sf.SetFill(0, 1);
                sf.SetAlign(2, 1);
                sf.SetAlign(3, 1);
                sf.SetAlign(4, 1);
                sf.SetAlign(5, 1);
                maxamt = maxqta = 0L;
                foreach (string itype in ((ipanelTypes[panels[0]].Count > 0) ? ipanelTypes[panels[0]] : types))
                {
                    header2 = " Asm ";
                    header5 = "Quota";
                    if (itype == "INGOT")
                    {
                        header2 = " Ref ";
                    }
                    else if (itype == "ORE")
                    {
                        header2 = " Ref ";
                        header5 = "Max";
                    }
                    if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                    sf.Add(0, "");
                    sf.Add(1, typeLabel[itype], true);
                    sf.Add(2, header2, true);
                    sf.Add(3, "Qty", true);
                    sf.Add(4, " / ", true);
                    sf.Add(5, header5, true);
                    sf.AddBlankRow();
                    foreach (ItemData data in typeSubData[itype].Values)
                    {
                        sf.Add(0, (data.amount == 0L) ? "0.0" : ("" + ((double)data.amount / data.quota)));
                        sf.Add(1, data.label, true);
                        text = ((data.producers.Count > 0) ? (data.producers.Count + " " + (data.producers.All(blk => (!(blk is IMyProductionBlock) || (blk as IMyProductionBlock).IsProducing)) ? " " : "!")) : ((data.hold > 0) ? "-  " : ""));
                        sf.Add(2, text, true);
                        sf.Add(3, (data.amount > 0L | data.quota > 0L) ? GetShorthand(data.amount) : "");
                        sf.Add(4, (data.quota > 0L) ? " / " : "", true);
                        sf.Add(5, (data.quota > 0L) ? GetShorthand(data.quota) : "");
                        maxamt = Math.Max(maxamt, data.amount);
                        maxqta = Math.Max(maxqta, data.quota);
                    }
                }
                sf.SetWidth(3, ScreenFormatter.GetWidth("8.88" + ((maxamt >= 1000000000000L) ? " M" : ((maxamt >= 1000000000L) ? " K" : "")), true));
                sf.SetWidth(5, ScreenFormatter.GetWidth("8.88" + ((maxqta >= 1000000000000L) ? " M" : ((maxqta >= 1000000000L) ? " K" : "")), true));
                foreach (IMyTextSurface panel in panels)
                    WriteTableToPanel("TIM Inventory", sf, panel, true);
            }
        } // UpdateInventoryPanels()


        void UpdateStatusPanels()
        {
            long r;
            StringBuilder sb;

            if (statusPanels.Count > 0)
            {
                sb = new StringBuilder();
                sb.Append(statsHeader);
                for (r = Math.Max(1, numCalls - statsLog.Length + 1); r <= numCalls; r++)
                    sb.Append(statsLog[r % statsLog.Length]);

                foreach (IMyTextSurface panel in statusPanels)
                {
                    panel.WritePublicTitle("Script Status", false);
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Status panels cannot be spanned");
                    panel.WritePublicText(sb.ToString(), false);
                    panel.ShowPublicTextOnScreen();
                }
            }

            if (debugPanels.Count > 0)
            {
                foreach (IMyTerminalBlock blockFrom in blockErrors.Keys)
                {
                    foreach (IMyTerminalBlock blockTo in blockErrors[blockFrom])
                        debugText.Add("No conveyor connection from " + blockFrom.CustomName + " to " + blockTo.CustomName);
                }
                foreach (IMyTextSurface panel in debugPanels)
                {
                    panel.WritePublicTitle("Script Debugging", false);
                    if (panelSpan.ContainsKey(panel))
                        debugText.Add("Debug panels cannot be spanned");
                    panel.WritePublicText(String.Join("\n", debugText), false);
                    panel.ShowPublicTextOnScreen();
                }
            }
            blockErrors.Clear();
        } // UpdateStatusPanels()


        void WriteTableToPanel(string title, ScreenFormatter sf, IMyTextSurface panel, bool allowspan = true, string before = "", string after = "")
        {
            int spanx, spany, rows, wide, size, width, height;
            int x, y, r;
            float fontsize;
            string[][] spanLines;
            string text;
            Matrix matrix;
            IMySlimBlock slim;
            IMyTextSurface spanpanel;

            // get the spanning dimensions, if any
            wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
            size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
            spanx = spany = 1;
            if (allowspan & panelSpan.ContainsKey(panel))
            {
                spanx = panelSpan[panel].a;
                spany = panelSpan[panel].b;
            }

            // reduce font size to fit everything
            x = sf.GetMinWidth();
            x = (x / spanx) + ((x % spanx > 0) ? 1 : 0);
            y = sf.GetNumRows();
            y = (y / spany) + ((y % spany > 0) ? 1 : 0);
            width = 658 * wide; // TODO monospace 26x17.5 chars
            fontsize = panel.GetValueFloat("FontSize");
            if (fontsize < 0.25f)
                fontsize = 1.0f;
            if (x > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(width * 100 / x) / 100.0f));
            if (y > 0)
                fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(1760 / y) / 100.0f));

            // calculate how much space is available on each panel
            width = (int)((float)width / fontsize);
            height = (int)(17.6f / fontsize);

            // write to each panel
            if (spanx > 1 | spany > 1)
            {
                spanLines = sf.ToSpan(width, spanx);
                matrix = new Matrix();
                panel.Orientation.GetMatrix(out matrix);
                for (x = 0; x < spanx; x++)
                {
                    r = 0;
                    for (y = 0; y < spany; y++)
                    {
                        slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position + x * wide * size * matrix.Right + y * size * matrix.Down));
                        if (slim != null && (slim.FatBlock is IMyTextSurface) && "" + slim.FatBlock.BlockDefinition == "" + panel.BlockDefinition)
                        {
                            spanpanel = slim.FatBlock as IMyTextSurface;
                            rows = Math.Max(0, spanLines[x].Length - r);
                            if (y + 1 < spany)
                                rows = Math.Min(rows, height);
                            text = "";
                            if (r < spanLines[x].Length)
                                text = String.Join("\n", spanLines[x], r, rows);
                            if (x == 0)
                                text += ((y == 0) ? before : (((y + 1) == spany) ? after : ""));
                            spanpanel.SetValueFloat("FontSize", fontsize);
                            spanpanel.WritePublicTitle(title + " (" + (x + 1) + "," + (y + 1) + ")", false);
                            spanpanel.WritePublicText(text, false);
                            spanpanel.ShowPublicTextOnScreen();
                        }
                        r += height;
                    }
                }
            }
            else
            {
                panel.SetValueFloat("FontSize", fontsize);
                panel.WritePublicTitle(title, false);
                panel.WritePublicText(before + sf.ToString(width) + after, false);
                panel.ShowPublicTextOnScreen();
            }
        } // WriteTableToPanel()


        /*
        * MAIN
        */


        public Program()
        {
            int ext;

            // parse stored data
            foreach (string line in Me.CustomData.Split(NEWLINE, REE))
            {
                string[] kv = line.Trim().Split('=');
                if (kv[0].Equals("TIM_version", OIC))
                {
                    if (!int.TryParse(kv[1], out lastVersion) | lastVersion > VERSION)
                    {
                        Echo("Invalid prior version: " + lastVersion);
                        lastVersion = 0;
                    }
                }
            }

            // initialize panel data
            ScreenFormatter.Init();
            statsHeader = (
                "Taleden's Inventory Manager\n" +
                "v" + VERS_MAJ + "." + VERS_MIN + "." + VERS_REV + " (" + VERS_UPD + ")\n\n" +
                ScreenFormatter.Format("Run", 80, out ext, 1) +
                ScreenFormatter.Format("Step", 125 + ext, out ext, 1) +
                ScreenFormatter.Format("Time", 145 + ext, out ext, 1) +
                ScreenFormatter.Format("Load", 105 + ext, out ext, 1) +
                ScreenFormatter.Format("S", 65 + ext, out ext, 1) +
                ScreenFormatter.Format("R", 65 + ext, out ext, 1) +
                ScreenFormatter.Format("A", 65 + ext, out ext, 1) +
                "\n\n"
            );

            // initialize default items, quotas, labels and blueprints
            // (TIM can also learn new items it sees in inventory)
            InitItems(DEFAULT_ITEMS);

            // initialize block:item restrictions
            // (TIM can also learn new restrictions whenever item transfers fail)
            InitBlockRestrictions(DEFAULT_RESTRICTIONS);

            Echo("Compiled TIM v" + VERS_MAJ + "." + VERS_MIN + "." + VERS_REV + " (" + VERS_UPD + ")");
        } // Program()


        public void Save()
        {
        } // Save()


        void Main(string argument)
        {
            // throttle interval
            if (numCalls > 0 & (sinceLast += Runtime.TimeSinceLastRun.TotalSeconds) < 0.5)
                return;
            sinceLast = 0.0;

            DateTime dtStart = DateTime.Now;
            int i, j, argCycle, step, time, load;
            bool argRewriteTags, argScanCollectors, argScanDrills, argScanGrinders, argScanWelders, argQuotaStable, toggle;
            char argTagOpen, argTagClose;
            string argTagPrefix, msg;
            StringBuilder sb = new StringBuilder();
            List<IMyTerminalBlock> blocks;

            // output terminal info
            numCalls++;
            Echo("Taleden's Inventory Manager");
            Echo("v" + VERS_MAJ + "." + VERS_MIN + "." + VERS_REV + " (" + VERS_UPD + ")");
            Echo("Last Run: #" + numCalls + " at " + dtStart.ToString("h:mm:ss tt"));
            if (lastVersion > 0 & lastVersion < VERSION)
                Echo("Upgraded from v" + (lastVersion / 1000000) + "." + (lastVersion / 1000 % 1000) + "." + (lastVersion % 1000));

            // reset status and debugging data every cycle
            debugText.Clear();
            debugLogic.Clear();
            step = numXfers = numRefs = numAsms = 0;

            // parse arguments
            toggle = true;
            argRewriteTags = REWRITE_TAGS;
            argTagOpen = TAG_OPEN;
            argTagClose = TAG_CLOSE;
            argTagPrefix = TAG_PREFIX;
            argCycle = CYCLE_LENGTH;
            argScanCollectors = SCAN_COLLECTORS;
            argScanDrills = SCAN_DRILLS;
            argScanGrinders = SCAN_GRINDERS;
            argScanWelders = SCAN_WELDERS;
            argQuotaStable = QUOTA_STABLE;
            foreach (string arg in argument.Split(SPACE, REE))
            {
                if (arg.Equals("rewrite", OIC))
                {
                    argRewriteTags = true;
                    debugText.Add("Tag rewriting enabled");
                }
                else if (arg.Equals("norewrite", OIC))
                {
                    argRewriteTags = false;
                    debugText.Add("Tag rewriting disabled");
                }
                else if (arg.StartsWith("tags=", OIC))
                {
                    msg = arg.Substring(5);
                    if (msg.Length != 2)
                    {
                        Echo("Invalid 'tags=' delimiters \"" + msg + "\": must be exactly two characters");
                        toggle = false;
                    }
                    else if (msg[0] == ' ' || msg[1] == ' ')
                    {
                        Echo("Invalid 'tags=' delimiters \"" + msg + "\": cannot be spaces");
                        toggle = false;
                    }
                    else if (char.ToUpper(msg[0]) == char.ToUpper(msg[1]))
                    {
                        Echo("Invalid 'tags=' delimiters \"" + msg + "\": characters must be different");
                        toggle = false;
                    }
                    else
                    {
                        argTagOpen = char.ToUpper(msg[0]);
                        argTagClose = char.ToUpper(msg[1]);
                        debugText.Add("Tags are delimited by \"" + argTagOpen + "\" and \"" + argTagClose + "\"");
                    }
                }
                else if (arg.StartsWith("prefix=", OIC))
                {
                    argTagPrefix = arg.Substring(7).Trim().ToUpper();
                    if (argTagPrefix == "")
                    {
                        debugText.Add("Tag prefix disabled");
                    }
                    else
                    {
                        debugText.Add("Tag prefix is \"" + argTagPrefix + "\"");
                    }
                }
                else if (arg.StartsWith("cycle=", OIC))
                {
                    if (int.TryParse(arg.Substring(6), out argCycle) == false || argCycle < 1)
                    {
                        Echo("Invalid 'cycle=' length \"" + arg.Substring(6) + "\": must be a positive integer");
                        toggle = false;
                    }
                    else
                    {
                        argCycle = Math.Min(Math.Max(argCycle, 1), MAX_CYCLE_STEPS);
                        if (argCycle < 2)
                        {
                            debugText.Add("Function cycling disabled");
                        }
                        else
                        {
                            debugText.Add("Cycle length is " + argCycle);
                        }
                    }
                }
                else if (arg.StartsWith("scan=", OIC))
                {
                    msg = arg.Substring(5);
                    if (msg.Equals("collectors", OIC))
                    {
                        argScanCollectors = true;
                        debugText.Add("Enabled scanning of Collectors");
                    }
                    else if (msg.Equals("drills", OIC))
                    {
                        argScanDrills = true;
                        debugText.Add("Enabled scanning of Drills");
                    }
                    else if (msg.Equals("grinders", OIC))
                    {
                        argScanGrinders = true;
                        debugText.Add("Enabled scanning of Grinders");
                    }
                    else if (msg.Equals("welders", OIC))
                    {
                        argScanWelders = true;
                        debugText.Add("Enabled scanning of Welders");
                    }
                    else
                    {
                        Echo("Invalid 'scan=' block type '" + msg + "': must be 'collectors', 'drills', 'grinders' or 'welders'");
                        toggle = false;
                    }
                }
                else if (arg.StartsWith("quota=", OIC))
                {
                    msg = arg.Substring(6);
                    if (msg.Equals("literal", OIC))
                    {
                        argQuotaStable = false;
                        debugText.Add("Disabled stable dynamic quotas");
                    }
                    else if (msg.Equals("stable", OIC))
                    {
                        argQuotaStable = true;
                        debugText.Add("Enabled stable dynamic quotas");
                    }
                    else
                    {
                        Echo("Invalid 'quota=' mode '" + msg + "': must be 'literal' or 'stable'");
                        toggle = false;
                    }
                }
                else if (arg.StartsWith("debug=", OIC))
                {
                    msg = arg.Substring(6);
                    if (msg.Length >= 1 & "quotas".StartsWith(msg, OIC))
                    {
                        debugLogic.Add("quotas");
                    }
                    else if (msg.Length >= 1 & "sorting".StartsWith(msg, OIC))
                    {
                        debugLogic.Add("sorting");
                    }
                    else if (msg.Length >= 1 & "refineries".StartsWith(msg, OIC))
                    {
                        debugLogic.Add("refineries");
                    }
                    else if (msg.Length >= 1 & "assemblers".StartsWith(msg, OIC))
                    {
                        debugLogic.Add("assemblers");
                    }
                    else
                    {
                        Echo("Invalid 'debug=' type '" + msg + "': must be 'quotas', 'sorting', 'refineries', or 'assemblers'");
                        toggle = false;
                    }
                }
                else
                {
                    Echo("Unrecognized argument: " + arg);
                    toggle = false;
                }
            }
            if (toggle == false)
                return;

            // apply changed arguments
            toggle = (tagOpen != argTagOpen) | (tagClose != argTagClose) | (tagPrefix != argTagPrefix);
            if ((toggle | (rewriteTags != argRewriteTags) | (cycleLength != argCycle)) && (cycleStep > 0))
            {
                cycleStep = 0;
                Echo(msg = "Options changed; cycle step reset.");
                debugText.Add(msg);
            }
            rewriteTags = argRewriteTags;
            tagOpen = argTagOpen;
            tagClose = argTagClose;
            tagPrefix = argTagPrefix;
            cycleLength = argCycle;
            if (tagRegex == null | toggle)
            {
                msg = "\\" + tagOpen;
                if (tagPrefix != "")
                {
                    msg += " *" + System.Text.RegularExpressions.Regex.Escape(tagPrefix) + "(|[ ,]+[^\\" + tagClose + "]*)";
                }
                else
                {
                    msg += "([^\\" + tagClose + "]*)";
                }
                msg += "\\" + tagClose;
                tagRegex = new System.Text.RegularExpressions.Regex(msg, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // scan connectors before PGs! if another TIM is on a grid that is *not* correctly docked, both still need to run
            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Scanning grid connectors ...");
                    debugText.Add(msg);
                }
                ScanGrids();
            }

            // search for other TIMs
            blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, (IMyTerminalBlock blk) => (blk == Me) | (tagRegex.IsMatch(blk.CustomName) & dockedgrids.Contains(blk.CubeGrid)));
            i = blocks.IndexOf(Me);
            j = blocks.FindIndex(block => block.IsFunctional & block.IsWorking);
            msg = tagOpen + tagPrefix + ((blocks.Count > 1) ? (" #" + (i + 1)) : "") + tagClose;
            Me.CustomName = tagRegex.IsMatch(Me.CustomName) ? tagRegex.Replace(Me.CustomName, msg, 1) : (Me.CustomName + " " + msg);
            if (i != j)
            {
                Echo("TIM #" + (j + 1) + " is on duty. Standing by.");
                if (("" + (blocks[j] as IMyProgrammableBlock).TerminalRunArgument).Trim() != ("" + Me.TerminalRunArgument).Trim())
                    Echo("WARNING: Script arguments do not match TIM #" + (j + 1) + ".");
                return;
            }

            // TODO: API testing
            /**
                GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks);
                Echo(""+blocks[0].GetInventory(0).Owner);
/**/

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Scanning inventories ...");
                    debugText.Add(msg);
                }

                // reset everything that we'll check during this step
                foreach (string itype in types)
                {
                    typeAmount[itype] = 0;
                    foreach (ItemData data in typeSubData[itype].Values)
                    {
                        data.amount = 0L;
                        data.avail = 0L;
                        data.locked = 0L;
                        data.invenTotal.Clear();
                        data.invenSlot.Clear();
                    }
                }
                blockTag.Clear();
                blockGtag.Clear();
                invenLocked.Clear();
                invenHidden.Clear();

                // scan inventories
                ScanGroups();
                ScanBlocks<IMyAssembler>();
                ScanBlocks<IMyCargoContainer>();
                if (argScanCollectors)
                    ScanBlocks<IMyCollector>();
                ScanBlocks<IMyGasGenerator>();
                ScanBlocks<IMyGasTank>();
                ScanBlocks<IMyReactor>();
                ScanBlocks<IMyRefinery>();
                ScanBlocks<IMyShipConnector>();
                ScanBlocks<IMyShipController>();
                if (argScanDrills)
                    ScanBlocks<IMyShipDrill>();
                if (argScanGrinders)
                    ScanBlocks<IMyShipGrinder>();
                if (argScanWelders)
                    ScanBlocks<IMyShipWelder>();
                ScanBlocks<IMyTextSurface>();
                ScanBlocks<IMyUserControllableGun>();

                // if we found any new item type/subtypes, re-sort the lists
                if (foundNewItem)
                {
                    foundNewItem = false;
                    types.Sort();
                    foreach (string itype in types)
                        typeSubs[itype].Sort();
                    subs.Sort();
                    foreach (string isub in subs)
                        subTypes[isub].Sort();
                }
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Scanning tags ...");
                    debugText.Add(msg);
                }

                // reset everything that we'll check during this step
                foreach (string itype in types)
                {
                    foreach (ItemData data in typeSubData[itype].Values)
                    {
                        data.qpriority = -1;
                        data.quota = 0L;
                        data.producers.Clear();
                    }
                }
                qpanelPriority.Clear();
                qpanelTypes.Clear();
                ipanelTypes.Clear();
                priTypeSubInvenRequest.Clear();
                statusPanels.Clear();
                debugPanels.Clear();
                refineryOres.Clear();
                assemblerItems.Clear();
                panelSpan.Clear();

                // parse tags
                ParseBlockTags();
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Adjusting tallies ...");
                    debugText.Add(msg);
                }
                AdjustAmounts();
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Scanning quota panels ...");
                    debugText.Add(msg);
                }
                ProcessQuotaPanels(argQuotaStable);
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Processing limited item requests ...");
                    debugText.Add(msg);
                }
                AllocateItems(true); // limited requests
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Managing refineries ...");
                    debugText.Add(msg);
                }
                ManageRefineries();
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Processing remaining item requests ...");
                    debugText.Add(msg);
                }
                AllocateItems(false); // unlimited requests
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Managing assemblers ...");
                    debugText.Add(msg);
                }
                ManageAssemblers();
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Scanning production ...");
                    debugText.Add(msg);
                }
                ScanProduction();
            }

            if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS)
            {
                if (cycleLength > 1)
                {
                    Echo(msg = "Updating inventory panels ...");
                    debugText.Add(msg);
                }
                UpdateInventoryPanels();

                // update persistent data after one full cycle
                Me.CustomData = "TIM_version=" + (lastVersion = VERSION);
            }

            if (step != MAX_CYCLE_STEPS)
                debugText.Add("ERROR: step" + step + " of " + MAX_CYCLE_STEPS);

            // update script status and debug panels on every cycle step
            cycleStep++;
            time = (int)((DateTime.Now - dtStart).TotalMilliseconds + 0.5);
            load = (int)(100.0f * Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount + 0.5);
            i = 0;
            statsLog[numCalls % statsLog.Length] = (
                ScreenFormatter.Format("" + numCalls, 80, out i, 1) +
                ScreenFormatter.Format(cycleStep + " / " + cycleLength, 125 + i, out i, 1, true) +
                ScreenFormatter.Format(time + " ms", 145 + i, out i, 1) +
                ScreenFormatter.Format(load + "%", 105 + i, out i, 1, true) +
                ScreenFormatter.Format("" + numXfers, 65 + i, out i, 1, true) +
                ScreenFormatter.Format("" + numRefs, 65 + i, out i, 1, true) +
                ScreenFormatter.Format("" + numAsms, 65 + i, out i, 1, true) +
                "\n"
            );
            Echo(msg = ((cycleLength > 1) ? ("Cycle " + cycleStep + " of " + cycleLength + " completed in ") : "Completed in ") + time + " ms, " + load + "% load (" + Runtime.CurrentInstructionCount + " instructions)");
            debugText.Add(msg);
            UpdateStatusPanels();
            if (cycleStep >= cycleLength)
                cycleStep = 0;

        } // Main()


        /*
        * ScreenFormatter
        */


        public class ScreenFormatter
        {
            private static Dictionary<char, byte> charWidth = new Dictionary<char, byte>();
            private static Dictionary<string, int> textWidth = new Dictionary<string, int>();
            private static byte SZ_SPACE;
            private static byte SZ_SHYPH;

            public static int GetWidth(string text, bool memoize = false)
            {
                int width;
                if (!textWidth.TryGetValue(text, out width))
                {
                    // this isn't faster (probably slower) but it's less "complex"
                    // according to SE's silly branch count metric
                    Dictionary<char, byte> cW = charWidth;
                    string t = text + "\0\0\0\0\0\0\0";
                    int i = t.Length - (t.Length % 8);
                    byte w0, w1, w2, w3, w4, w5, w6, w7;
                    while (i > 0)
                    {
                        cW.TryGetValue(t[i - 1], out w0);
                        cW.TryGetValue(t[i - 2], out w1);
                        cW.TryGetValue(t[i - 3], out w2);
                        cW.TryGetValue(t[i - 4], out w3);
                        cW.TryGetValue(t[i - 5], out w4);
                        cW.TryGetValue(t[i - 6], out w5);
                        cW.TryGetValue(t[i - 7], out w6);
                        cW.TryGetValue(t[i - 8], out w7);
                        width += w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7;
                        i -= 8;
                    }
                    if (memoize)
                        textWidth[text] = width;
                }
                return width;
            } // GetWidth()

            public static string Format(string text, int width, out int unused, int align = -1, bool memoize = false)
            {
                int spaces, bars;

                // '\u00AD' is a "soft hyphen" in UTF16 but Panels don't wrap lines so
                // it's just a wider space character ' ', useful for column alignment
                unused = width - GetWidth(text, memoize);
                if (unused <= SZ_SPACE / 2)
                    return text;
                spaces = unused / SZ_SPACE;
                bars = 0;
                unused -= spaces * SZ_SPACE;
                if (2 * unused <= SZ_SPACE + (spaces * (SZ_SHYPH - SZ_SPACE)))
                {
                    bars = Math.Min(spaces, (int)((float)unused / (SZ_SHYPH - SZ_SPACE) + 0.4999f));
                    spaces -= bars;
                    unused -= bars * (SZ_SHYPH - SZ_SPACE);
                }
                else if (unused > SZ_SPACE / 2)
                {
                    spaces++;
                    unused -= SZ_SPACE;
                }
                if (align > 0)
                    return new String(' ', spaces) + new String('\u00AD', bars) + text;
                if (align < 0)
                    return text + new String('\u00AD', bars) + new String(' ', spaces);
                if ((spaces % 2) > 0 & (bars % 2) == 0)
                    return new String(' ', spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars / 2) + new String(' ', spaces - (spaces / 2));
                return new String(' ', spaces - (spaces / 2)) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars - (bars / 2)) + new String(' ', spaces / 2);
            } // Format()

            public static string Format(double value, int width, out int unused)
            {
                int spaces, bars;
                value = Math.Min(Math.Max(value, 0.0f), 1.0f);
                spaces = width / SZ_SPACE;
                bars = (int)(spaces * value + 0.5f);
                unused = width - (spaces * SZ_SPACE);
                return new String('I', bars) + new String(' ', spaces - bars);
            } // Format()

            public static void Init()
            {
                InitChars(0, "\u2028\u2029\u202F");
                InitChars(7, "'|\u00A6\u02C9\u2018\u2019\u201A");
                InitChars(8, "\u0458");
                InitChars(9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
                InitChars(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");
                InitChars(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");
                InitChars(12, "*\u00B2\u00B3\u00B9");
                InitChars(13, "\\\u00B0\u201C\u201D\u201E");
                InitChars(14, "\u0491");
                InitChars(15, "/\u0133\u0442\u044D\u0454");
                InitChars(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
                InitChars(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
                InitChars(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F");
                InitChars(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
                InitChars(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC");
                InitChars(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
                InitChars(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
                InitChars(23, "\u0459");
                InitChars(24, "\u044E");
                InitChars(25, "%\u0132\u042B");
                InitChars(26, "@\u00A9\u00AE\u043C\u0448\u045A");
                InitChars(27, "M\u041C\u0428");
                InitChars(28, "mw\u00BC\u0175\u042E\u0449");
                InitChars(29, "\u00BE\u00E6\u0153\u0409");
                InitChars(30, "\u00BD\u0429");
                InitChars(31, "\u2122");
                InitChars(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
                SZ_SPACE = charWidth[' '];
                SZ_SHYPH = charWidth['\u00AD'];
            } // Init()

            private static void InitChars(byte width, string text)
            {
                // more silly loop-unrolling, as in GetWidth()
                Dictionary<char, byte> cW = charWidth;
                string t = text + "\0\0\0\0\0\0\0";
                byte w = Math.Max((byte)0, width);
                int i = t.Length - (t.Length % 8);
                while (i > 0)
                {
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                    cW[t[--i]] = w;
                }
                cW['\0'] = 0;
            } // InitChars()

            private int numCols;
            private int numRows;
            private int padding;
            private List<string>[] colRowText;
            private List<int>[] colRowWidth;
            private int[] colAlign;
            private int[] colFill;
            private bool[] colBar;
            private int[] colWidth;

            public ScreenFormatter(int numCols, int padding = 1)
            {
                this.numCols = numCols;
                this.numRows = 0;
                this.padding = padding;
                this.colRowText = new List<string>[numCols];
                this.colRowWidth = new List<int>[numCols];
                this.colAlign = new int[numCols];
                this.colFill = new int[numCols];
                this.colBar = new bool[numCols];
                this.colWidth = new int[numCols];
                for (int c = 0; c < numCols; c++)
                {
                    this.colRowText[c] = new List<string>();
                    this.colRowWidth[c] = new List<int>();
                    this.colAlign[c] = -1;
                    this.colFill[c] = 0;
                    this.colBar[c] = false;
                    this.colWidth[c] = 0;
                }
            } // ScreenFormatter()

            public void Add(int col, string text, bool memoize = false)
            {
                int width = 0;
                this.colRowText[col].Add(text);
                if (this.colBar[col] == false)
                {
                    width = GetWidth(text, memoize);
                    this.colWidth[col] = Math.Max(this.colWidth[col], width);
                }
                this.colRowWidth[col].Add(width);
                this.numRows = Math.Max(this.numRows, this.colRowText[col].Count);
            } // Add()

            public void AddBlankRow()
            {
                for (int c = 0; c < this.numCols; c++)
                {
                    this.colRowText[c].Add("");
                    this.colRowWidth[c].Add(0);
                }
                this.numRows++;
            } // AddBlankRow()

            public int GetNumRows()
            {
                return this.numRows;
            } // GetNumRows()

            public int GetMinWidth()
            {
                int width = this.padding * SZ_SPACE;
                for (int c = 0; c < this.numCols; c++)
                    width += this.padding * SZ_SPACE + this.colWidth[c];
                return width;
            } // GetMinWidth()

            public void SetAlign(int col, int align)
            {
                this.colAlign[col] = align;
            } // SetAlign()

            public void SetFill(int col, int fill = 1)
            {
                this.colFill[col] = fill;
            } // SetFill()

            public void SetBar(int col, bool bar = true)
            {
                this.colBar[col] = bar;
            } // SetBar()

            public void SetWidth(int col, int width)
            {
                this.colWidth[col] = width;
            } // SetWidth()

            public string[][] ToSpan(int width = 0, int span = 1)
            {
                int c, r, s, i, j, textwidth, unused, remaining;
                int[] colWidth;
                byte w;
                double value;
                string text;
                StringBuilder sb;
                string[][] spanLines;

                // clone the user-defined widths and tally fill columns
                colWidth = (int[])this.colWidth.Clone();
                unused = width * span - this.padding * SZ_SPACE;
                remaining = 0;
                for (c = 0; c < this.numCols; c++)
                {
                    unused -= this.padding * SZ_SPACE;
                    if (this.colFill[c] == 0)
                        unused -= colWidth[c];
                    remaining += this.colFill[c];
                }

                // distribute remaining width to fill columns
                for (c = 0; c < this.numCols & remaining > 0; c++)
                {
                    if (this.colFill[c] > 0)
                    {
                        colWidth[c] = Math.Max(colWidth[c], this.colFill[c] * unused / remaining);
                        unused -= colWidth[c];
                        remaining -= this.colFill[c];
                    }
                }

                // initialize output arrays
                spanLines = new string[span][];
                for (s = 0; s < span; s++)
                    spanLines[s] = new string[this.numRows];
                span--; // make "span" inclusive so "s < span" implies one left

                // render all rows and columns
                i = 0;
                sb = new StringBuilder();
                for (r = 0; r < this.numRows; r++)
                {
                    sb.Clear();
                    s = 0;
                    remaining = width;
                    unused = 0;
                    for (c = 0; c < this.numCols; c++)
                    {
                        unused += this.padding * SZ_SPACE;
                        if (r >= this.colRowText[c].Count || colRowText[c][r] == "")
                        {
                            unused += colWidth[c];
                        }
                        else
                        {
                            // render the bar, or fetch the cell text
                            text = this.colRowText[c][r];
                            charWidth.TryGetValue(text[0], out w);
                            textwidth = this.colRowWidth[c][r];
                            if (this.colBar[c] == true)
                            {
                                value = 0.0;
                                if (double.TryParse(text, out value))
                                    value = Math.Min(Math.Max(value, 0.0), 1.0);
                                i = (int)((colWidth[c] / SZ_SPACE) * value + 0.5);
                                w = SZ_SPACE;
                                textwidth = i * SZ_SPACE;
                            }

                            // if the column is not left-aligned, calculate left spacing
                            if (this.colAlign[c] > 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (this.colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) / 2;
                            }

                            // while the left spacing leaves no room for text, adjust it
                            while (s < span & unused > remaining - w)
                            {
                                sb.Append(' ');
                                spanLines[s][r] = sb.ToString();
                                sb.Clear();
                                s++;
                                unused -= remaining;
                                remaining = width;
                            }

                            // add left spacing
                            remaining -= unused;
                            sb.Append(Format("", unused, out unused));
                            remaining += unused;

                            // if the column is not right-aligned, calculate right spacing
                            if (this.colAlign[c] < 0)
                            {
                                unused += (colWidth[c] - textwidth);
                            }
                            else if (this.colAlign[c] == 0)
                            {
                                unused += (colWidth[c] - textwidth) - ((colWidth[c] - textwidth) / 2);
                            }

                            // while the bar or text runs to the next span, split it
                            if (this.colBar[c] == true)
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    j = remaining / SZ_SPACE;
                                    remaining -= j * SZ_SPACE;
                                    textwidth -= j * SZ_SPACE;
                                    sb.Append(new String('I', j));
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    i -= j;
                                }
                                text = new String('I', i);
                            }
                            else
                            {
                                while (s < span & textwidth > remaining)
                                {
                                    i = 0;
                                    while (remaining >= w)
                                    {
                                        remaining -= w;
                                        textwidth -= w;
                                        charWidth.TryGetValue(text[++i], out w);
                                    }
                                    sb.Append(text, 0, i);
                                    spanLines[s][r] = sb.ToString();
                                    sb.Clear();
                                    s++;
                                    unused -= remaining;
                                    remaining = width;
                                    text = text.Substring(i);
                                }
                            }

                            // add cell text
                            remaining -= textwidth;
                            sb.Append(text);
                        }
                    }
                    spanLines[s][r] = sb.ToString();
                }

                return spanLines;
            } // ToSpan()

            public string ToString(int width = 0)
            {
                return String.Join("\n", this.ToSpan(width, 1)[0]);
            } // ToString()

        } // ScreenFormatter
    }
}
