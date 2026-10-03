using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TORBLTChaosDiplomacy;
namespace TaleWorlds.Library { public static class Debug { public static void Print(string text) {} } }
namespace TaleWorlds.MountAndBlade { public class MBSubModuleBase { protected virtual void OnSubModuleLoad() {} } }
namespace TaleWorlds.CampaignSystem {
 public class CampaignGameStarter {}
 public class CultureObject { public string StringId {get;set;} }
 public class Hero { public bool Adopted; }
 public interface IFaction { CultureObject Culture {get;} }
 public class Kingdom : IFaction { public CultureObject Culture {get;set;} public Hero Leader {get;set;} }
 public class Clan : IFaction { public CultureObject Culture {get;set;} public Hero Leader {get;set;} public Kingdom Kingdom {get;set;} }
 public static class FactionManager { public static int Calls; public static void DeclareWar(IFaction a, IFaction b) {Calls++;} }
}
namespace BLTAdoptAHero { public static class HeroExtensions { public static bool IsAdopted(this TaleWorlds.CampaignSystem.Hero hero) => hero.Adopted; } }
class Test {
 static int assertions;
 static void Check(bool condition,string name) { if(!condition) throw new Exception(name); assertions++; }
 static void Main() {
  var chaos = new TaleWorlds.CampaignSystem.CultureObject {StringId="chaos_culture"};
  var normal = new TaleWorlds.CampaignSystem.CultureObject {StringId="empire"};
  var viewer=new TaleWorlds.CampaignSystem.Hero {Adopted=true};
  var npc=new TaleWorlds.CampaignSystem.Hero();
  var viewerKingdom=new TaleWorlds.CampaignSystem.Kingdom {Culture=chaos,Leader=viewer};
  var npcKingdom=new TaleWorlds.CampaignSystem.Kingdom {Culture=chaos,Leader=npc};
  var other=new TaleWorlds.CampaignSystem.Kingdom {Culture=normal,Leader=viewer};
  Check(ChaosWarPatch.IsViewerChaosFaction(viewerKingdom),"Viewer Chaos kingdom protected");
  Check(!ChaosWarPatch.IsViewerChaosFaction(npcKingdom),"NPC Chaos kingdom unchanged");
  Check(!ChaosWarPatch.IsViewerChaosFaction(other),"Non Chaos viewer unchanged");
  Check(!ChaosWarPatch.IsViewerChaosFaction(null),"Null faction");
  Check(ChaosWarPatch.IsViewerChaosFaction(new TaleWorlds.CampaignSystem.Clan {Culture=chaos,Leader=viewer}),"Independent viewer clan");
  Check(!ChaosWarPatch.IsViewerChaosFaction(new TaleWorlds.CampaignSystem.Clan {Culture=chaos,Leader=viewer,Kingdom=npcKingdom}),"Viewer vassal cannot shield NPC kingdom");
  Check(ChaosWarPatch.IsViewerChaosFaction(new TaleWorlds.CampaignSystem.Clan {Culture=chaos,Leader=npc,Kingdom=viewerKingdom}),"Clan resolves to viewer kingdom");
  var guard=typeof(ChaosWarPatch).GetMethod("DeclareWarUnlessViewerChaos",BindingFlags.NonPublic|BindingFlags.Static);
  guard.Invoke(null,new object[]{viewerKingdom,other}); guard.Invoke(null,new object[]{other,viewerKingdom});
  Check(TaleWorlds.CampaignSystem.FactionManager.Calls==0,"Both declaration directions protected");
  guard.Invoke(null,new object[]{npcKingdom,other}); Check(TaleWorlds.CampaignSystem.FactionManager.Calls==1,"NPC declaration preserved");
  var original=typeof(TaleWorlds.CampaignSystem.FactionManager).GetMethod("DeclareWar");
  var transpiler=typeof(ChaosWarPatch).GetMethod("Transpiler",BindingFlags.NonPublic|BindingFlags.Static);
  var instructions=new List<CodeInstruction> {new CodeInstruction(OpCodes.Nop),new CodeInstruction(OpCodes.Call,original),new CodeInstruction(OpCodes.Call,original),new CodeInstruction(OpCodes.Call,original)};
  var label=new DynamicMethod("labels",typeof(void),Type.EmptyTypes).GetILGenerator().DefineLabel(); instructions[1].labels.Add(label);
  var rewritten=((IEnumerable<CodeInstruction>)transpiler.Invoke(null,new object[]{instructions})).ToList();
  Check(rewritten.Count==4 && rewritten.Skip(1).All(i=>i.Calls(guard)),"All three war sites guarded");
  Check(rewritten[1].labels.Contains(label),"Branch labels preserved");
  try {transpiler.Invoke(null,new object[]{new[]{new CodeInstruction(OpCodes.Call,original)}}); throw new Exception("Missing mismatch rejection");}
  catch(TargetInvocationException e) {Check(e.InnerException is InvalidOperationException,"Unexpected TOR shape rejected");}
  Console.WriteLine("Passed " + assertions + " diplomacy and transpiler assertions.");
 }
}
