using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using FM.Graphics;
using FM.Graphics.CharacterBuilder.KitCreator;
using FM.Match;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace FM26CustomKits;

[BepInPlugin(Guid, "FM26 Custom Kits", "0.3.0")]
public class Plugin : BasePlugin
{
    public const string Guid = "fm26.community.customkits";
    private const string CustomPrefix = "customkit:";

    internal new static ManualLogSource Log;
    internal static string Kits3dDir;
    internal static string Kits2dDir;
    internal static string ReferenciaFile;
    internal static ConfigEntry<bool> LogKitRequests;
    internal static readonly Dictionary<string, string> TextReplacements = new();
    internal static System.Text.RegularExpressions.Regex NamePattern;

    private static readonly Dictionary<string, Texture2D> TextureCache = new();
    internal static readonly HashSet<string> LoggedKeys = new();

    // referencia.txt: { "339:home" → linha formatada }
    private static readonly Dictionary<string, string> _refTeams = new();
    private static readonly Dictionary<string, string> _refSigs  = new();

    public override void Load()
    {
        Log = base.Log;
        var root = Path.Combine(Paths.PluginPath, "FM26CustomKits");
        Kits3dDir = Path.Combine(root, "kits3d");
        Directory.CreateDirectory(Kits3dDir);
        Kits2dDir = Path.Combine(root, "kits2d");
        Directory.CreateDirectory(Kits2dDir);
        ReferenciaFile = Path.Combine(root, "referencia.txt");

        LogKitRequests = Config.Bind("Debug", "LogKitRequests", true,
            "Registra no log cada time/uniforme pedido, para descobrir o ID dos times.");

        var namesFile = Path.Combine(root, "nomes.txt");
        if (!File.Exists(namesFile))
            File.WriteAllText(namesFile,
                "# SIGLA=Nome exibido (uma por linha). Troca a sigla como palavra inteira, inclusive dentro de frases.\n" +
                "# ATENÇÃO: não coloque BRA (sigla da nacionalidade Brasil — trocaria a nacionalidade de todo jogador).\n" +
                "# Confira as siglas na tabela do seu save antes de ativar — podem variar entre bases.\n" +
                "ATM=Atlético-MG\n" +
                "ATP=Athletico-PR\n" +
                "BAH=Bahia\n" +
                "BOT=Botafogo\n" +
                "CEA=Ceará\n" +
                "CEC=Cruzeiro\n" +
                "COR=Corinthians\n" +
                "CTB=Coritiba\n" +
                "CUI=Cuiabá\n" +
                "FLA=Flamengo\n" +
                "FLU=Fluminense\n" +
                "FOR=Fortaleza\n" +
                "GOI=Goiás\n" +
                "GRE=Grêmio\n" +
                "INT=Internacional\n" +
                "MFC=Mirassol\n" +
                "SAN=Santos\n" +
                "SEP=Palmeiras\n" +
                "SPO=São Paulo\n" +
                "VDG=Vasco da Gama\n" +
                "VIT=Vitória\n");
        foreach (var line in File.ReadAllLines(namesFile))
        {
            var idx = line.IndexOf('=');
            if (line.StartsWith('#') || idx <= 0) continue;
            TextReplacements[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        if (TextReplacements.Count > 0)
        {
            var alternatives = string.Join("|", TextReplacements.Keys.Select(System.Text.RegularExpressions.Regex.Escape));
            NamePattern = new System.Text.RegularExpressions.Regex(
                $@"(?<![\p{{L}}\p{{N}}])(?:{alternatives})(?![\p{{L}}\p{{N}}])",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        var harmony = new Harmony(Guid);
        foreach (var type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
        {
            try { harmony.CreateClassProcessor(type).Patch(); }
            catch (Exception ex) { Log.LogError($"Falha ao interceptar {type.Name}: {ex.GetBaseException().Message}"); }
        }
        foreach (var m in harmony.GetPatchedMethods())
            Log.LogInfo($"Interceptado: {m.DeclaringType?.Name}.{m.Name}");
        Log.LogInfo($"Carregado. Camisas 3D em: {Kits3dDir} | {TextReplacements.Count} troca(s) de nome.");
    }

    internal static void LogOnce(string key, string message)
    {
        if (LogKitRequests.Value && LoggedKeys.Add(key)) Log.LogInfo(message);
    }

    internal static string KitSuffix(GraphicsUtils.KitType kitType) => kitType switch
    {
        GraphicsUtils.KitType.Home => "home",
        GraphicsUtils.KitType.Away => "away",
        GraphicsUtils.KitType.Third => "third",
        _ => "generic",
    };

    internal static string FindCustomKit(long teamId, GraphicsUtils.KitType kitType)
    {
        var path = Path.Combine(Kits3dDir, $"{teamId}_{KitSuffix(kitType)}.png");
        return File.Exists(path) ? path : null;
    }

    internal static string FindCustomKit2D(string signature)
    {
        var path = Path.Combine(Kits2dDir, $"{signature}.png");
        return File.Exists(path) ? path : null;
    }

    internal static void RecordTeam(long teamId, string suffix, string kitPath)
    {
        var key    = $"{teamId}:{suffix}";
        var status = kitPath != null ? $"instalada  ({Path.GetFileName(kitPath)})" : "sem camisa";
        var line   = $"{teamId,-14}{suffix,-10}{status}";
        if (_refTeams.TryGetValue(key, out var prev) && prev == line) return;
        _refTeams[key] = line;
        WriteReferencia();
    }

    internal static void RecordSig(string sig, GraphicsUtils.ResourceType tipo, string uniforme, bool hasCamisa)
    {
        var label = tipo switch
        {
            GraphicsUtils.ResourceType.Kit2DFront    => "frente (tela do clube)   ",
            GraphicsUtils.ResourceType.Kit2DOutfield => "linha  (tática/escalação)",
            GraphicsUtils.ResourceType.Kit2DGK       => "goleiro (tática)         ",
            _ => $"{tipo,-29}",
        };
        var status = hasCamisa ? "instalada" : "sem camisa";
        var line   = $"{sig,-12}{label}  {uniforme,-10}{status}";
        if (_refSigs.TryGetValue(sig, out var prev) && prev == line) return;
        _refSigs[sig] = line;
        WriteReferencia();
    }

    private static void WriteReferencia()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# FM26 Custom Kits — referência gerada automaticamente");
            sb.AppendLine("# Atualizado enquanto você joga. Abra a qualquer momento.");
            sb.AppendLine();
            sb.AppendLine("## CAMISAS 3D");
            sb.AppendLine("# Coloque em:  kits3d/<ID>_home.png | _away.png | _third.png | _gk.png");
            sb.AppendLine($"{"ID",-14}{"tipo",-10}status");
            sb.AppendLine(new string('-', 54));
            foreach (var v in _refTeams.Values.OrderBy(x => x))
                sb.AppendLine(v);
            sb.AppendLine();
            sb.AppendLine("## ASSINATURAS 2D");
            sb.AppendLine("# Coloque em:  kits2d/<assinatura>.png  (256x256, fundo transparente)");
            sb.AppendLine($"{"assinatura",-12}{"tipo",-31}  {"uniforme",-10}status");
            sb.AppendLine(new string('-', 72));
            foreach (var v in _refSigs.Values.OrderBy(x => x))
                sb.AppendLine(v);
            File.WriteAllText(ReferenciaFile, sb.ToString());
        }
        catch (Exception ex)
        {
            Log.LogWarning($"[Ref] Falha ao escrever referencia.txt: {ex.Message}");
        }
    }

    internal static string DescribeColours(SortedDictionary<uint, string> props) =>
        string.Join(" ", props.Values.Where(v => v != null && v.StartsWith("RGBA")).Distinct()
            .Select(v => v.Replace("RGBA(", "").Replace(", 255)", "").Replace(", ", ",")));

    internal static bool IsCustom(string resourceId) =>
        resourceId != null && resourceId.StartsWith(CustomPrefix, StringComparison.Ordinal);

    internal static string ToResourceId(string path) => CustomPrefix + path;

    internal static Texture2D LoadCustomTexture(string resourceId)
    {
        if (TextureCache.TryGetValue(resourceId, out var cached) && cached != null) return cached;

        var path = resourceId[CustomPrefix.Length..];
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, false)
        {
            name = Path.GetFileNameWithoutExtension(path),
            hideFlags = HideFlags.HideAndDontSave,
        };
        if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), false))
        {
            Log.LogError($"Falha ao ler PNG: {path}");
            return null;
        }
        TextureCache[resourceId] = tex;
        Log.LogInfo($"Camisa carregada: {path} ({tex.width}x{tex.height})");
        return tex;
    }
}

[HarmonyPatch(typeof(KitMaterials), nameof(KitMaterials.TryGetLicensedKitTextureResourceID))]
internal static class LicensedKitIdPatch
{
    private static void Postfix(long teamID, GraphicsUtils.KitType kitType, ref string resourceID, ref bool __result)
    {
        Plugin.LogOnce($"{teamID}:{kitType}",
            $"[Kit] time={teamID} uniforme={Plugin.KitSuffix(kitType)} licenciado={__result} recurso={resourceID}");

        var custom = Plugin.FindCustomKit(teamID, kitType);
        Plugin.RecordTeam(teamID, Plugin.KitSuffix(kitType), custom);
        if (custom == null) return;
        resourceID = Plugin.ToResourceId(custom);
        __result = true;
    }
}

[HarmonyPatch(typeof(MatchCharacterFactory), "TryGetKitTexture")]
internal static class MatchKitTexturePatch
{
    private static bool Prefix(string resourceID, ref Texture2D kitTexture, ref bool __result)
    {
        Plugin.LogOnce("tex:" + resourceID, $"[Kit] Partida pediu textura: {resourceID}");
        if (!Plugin.IsCustom(resourceID)) return true;
        kitTexture = Plugin.LoadCustomTexture(resourceID);
        __result = kitTexture != null;
        return false;
    }
}

[HarmonyPatch(typeof(MatchCharacterFactory), nameof(MatchCharacterFactory.CreatePlayerKit))]
internal static class CreatePlayerKitTrace
{
    private static void Prefix() => Plugin.LogOnce("trace:CreatePlayerKit", "[Kit] CreatePlayerKit chamado (partida 3D montando jogadores)");
}

[HarmonyPatch]
internal static class MatchKitTrace
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        var targets = new List<System.Reflection.MethodBase>();
        targets.AddRange(Named(typeof(Match3DBuilder), "InitializeCharacters"));
        targets.AddRange(Named(typeof(MatchCharacterFactory), "CreateMatchCharacter"));
        targets.AddRange(Named(typeof(MatchCharacterFactory), "CreateKitAndCopyToTextureArray"));
        targets.AddRange(Named(typeof(PlayerVisuals3D), "Initialise"));
        targets.AddRange(Named(typeof(KitMaterials), nameof(KitMaterials.ApplyLicensedKitWithNumberToTextureArray)));
        return targets;
    }

    private static IEnumerable<System.Reflection.MethodInfo> Named(Type type, string name) =>
        AccessTools.GetDeclaredMethods(type).Where(m => m.Name == name);

    private static void Prefix(System.Reflection.MethodBase __originalMethod) =>
        Plugin.LogOnce("trace:" + __originalMethod.Name, $"[Kit] Trace: {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}");
}

[HarmonyPatch(typeof(GraphicsUtils), nameof(GraphicsUtils.ResolveKitResourceID))]
internal static class ResolveKitResourceIdPatch
{
    internal static string PendingCustomKit;

    private static void Postfix(long __0, GraphicsUtils.KitType __1, string __result)
    {
        Plugin.LogOnce($"resolve:{__0}:{__1}", $"[Kit] ResolveKitResourceID time={__0} uniforme={Plugin.KitSuffix(__1)} recurso={__result}");
        PendingCustomKit = Plugin.FindCustomKit(__0, __1);
    }
}

[HarmonyPatch(typeof(Team), MethodType.Constructor,
    new[] { typeof(FixtureTeam), typeof(SI.Match.TeamKitSetup), typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<SI.Match.CrowdKitSetup>) })]
internal static class MatchTeamKitPatch
{
    internal static readonly Dictionary<string, string> CustomByKit = new();

    internal static string KitKey(SI.Match.PlayerKitSetup k)
    {
        static string C(Color c) => $"{c.r:F3},{c.g:F3},{c.b:F3}";
        return $"{k.shirtStyle}|{k.shortsStyle}|{k.socksStyle}|{C(k.shirtBackgroundColor)}|{C(k.shirtForegroundColor)}|{C(k.shirtOutlineColor)}|{C(k.shortsBackgroundColor)}|{C(k.shortsForegroundColor)}|{C(k.socksBackgroundColor)}";
    }

    private static void Postfix(SI.Match.TeamKitSetup teamKit)
    {
        var kitType = (GraphicsUtils.KitType)teamKit.kitType;
        var outfield = Plugin.FindCustomKit(teamKit.teamID, kitType);
        var gkPath = Path.Combine(Plugin.Kits3dDir, $"{teamKit.teamID}_gk.png");
        var gk = File.Exists(gkPath) ? gkPath : null;
        Plugin.LogOnce($"team:{teamKit.teamID}:{kitType}",
            $"[Kit] Partida: time={teamKit.teamID} uniforme={Plugin.KitSuffix(kitType)} linha={(outfield == null ? "-" : Path.GetFileName(outfield))} goleiro={(gk == null ? "-" : Path.GetFileName(gk))}");
        if (outfield != null) CustomByKit[KitKey(teamKit.playerKit)] = outfield;
        if (gk != null) CustomByKit[KitKey(teamKit.goalKeeperKit)] = gk;
    }
}

[HarmonyPatch(typeof(KitMaterials), nameof(KitMaterials.ApplyGenericKitToTextureArray))]
internal static class GenericKitReplacePatch
{
    // Maps PlayerKitSetup color key → custom kit path, populated at the malha=False setup call
    private static readonly Dictionary<string, string> KitBySetup = new();
    private static bool _pendingConsumed;

    private static Mesh _emptyMesh;
    private static Texture2D _blankNumber;

    private static Mesh EmptyMesh => _emptyMesh ??= new Mesh { name = "customkit-empty", hideFlags = HideFlags.HideAndDontSave };

    private static Texture2D BlankNumber
    {
        get
        {
            if (_blankNumber != null) return _blankNumber;
            _blankNumber = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            _blankNumber.SetPixels32(new Color32[] { default, default, default, default });
            _blankNumber.Apply();
            return _blankNumber;
        }
    }

    private static bool Prefix(SI.Match.PlayerKitSetup __0, RenderTextureDescriptor __2, Texture2DArray __3, int __4, Texture2D __5, Mesh __6,
        ref Il2CppSystem.Collections.IEnumerator __result)
    {
        bool hasMesh = __6 != null;
        var setupKey = MatchTeamKitPatch.KitKey(__0);

        if (!hasMesh)
        {
            // Setup call (malha=False): bind PendingCustomKit to this setup's color key
            var pending = ResolveKitResourceIdPatch.PendingCustomKit;
            if (pending != null)
            {
                KitBySetup[setupKey] = pending;
                _pendingConsumed = false;
            }
        }
        else if (!_pendingConsumed)
        {
            // First real render call (malha=True): release PendingCustomKit so the next team doesn't inherit it
            ResolveKitResourceIdPatch.PendingCustomKit = null;
            _pendingConsumed = true;
        }

        string custom = null;
        KitBySetup.TryGetValue(setupKey, out custom);
        if (custom == null) MatchTeamKitPatch.CustomByKit.TryGetValue(setupKey, out custom);

        Plugin.LogOnce($"generic:{custom ?? "-"}:{hasMesh}:{__5 != null}",
            $"[Kit] Camisa genérica pedida: custom={(custom == null ? "-" : Path.GetFileName(custom))} malha={hasMesh} numero={__5 != null} slot={__4}");
        if (custom == null) return true;

        var tex = Plugin.LoadCustomTexture(Plugin.ToResourceId(custom));
        if (tex == null) return true;
        try
        {
            KitMaterials.ApplyLicensedKitWithNumberToTextureArray(tex, __5 ?? BlankNumber, __6 ?? EmptyMesh, __2, __3, __4);
            __result = new Il2CppSystem.Collections.ArrayList().GetEnumerator();
            Plugin.LogOnce("applied:" + custom + ":" + __4, $"[Kit] Camisa 3D aplicada: {Path.GetFileName(custom)} (slot {__4})");
            return false;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[Kit] Falha ao aplicar {Path.GetFileName(custom)}: {ex.GetBaseException().Message}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(FM.Graphics.Environment.EnvironmentRecolouring), "UpdateKitTexture2DArrayWrapper")]
internal static class EnvironmentKitPatch
{
    private static bool Prefix(string __0)
    {
        if (!Plugin.IsCustom(__0)) return true;
        Plugin.LogOnce("env:" + __0, $"[Kit] Cenário pediu camisa customizada (ignorado por enquanto): {__0}");
        return false;
    }
}

[HarmonyPatch(typeof(FM.UI.DatabaseObjectImageHelpers), nameof(FM.UI.DatabaseObjectImageHelpers.ConvertDynamicReferenceToAddressableID),
    new[] { typeof(SI.Bindable.DynamicReference), typeof(GraphicsUtils.ResourceType), typeof(string), typeof(bool), typeof(Il2CppSystem.Object) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
internal static class Kit2DIdPatch
{
    private static void Postfix(SI.Bindable.DynamicReference __0, GraphicsUtils.ResourceType __1, string __2, ref string __result)
    {
        if (__1 is not (GraphicsUtils.ResourceType.Kit2DFront or GraphicsUtils.ResourceType.Kit2DOutfield or GraphicsUtils.ResourceType.Kit2DGK)) return;
        if (__0 == null) return;

        var props = new SortedDictionary<uint, string>();
        foreach (var kv in __0)
            props[kv.Key] = kv.Value?.AsString();
        var raw = $"{__1}|{__result}|" + string.Join(";", props.Select(p => $"{p.Key}={p.Value}"));
        var signature = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(raw)))[..8].ToLowerInvariant();

        if (Plugin.LogKitRequests.Value && Plugin.LoggedKeys.Add("2d:" + signature))
            Plugin.Log.LogInfo($"[Kit2D] assinatura={signature} tipo={__1} uniforme={__2} recurso={__result} cores={Plugin.DescribeColours(props)}");

        var custom = Plugin.FindCustomKit2D(signature);
        Plugin.RecordSig(signature, __1, __2 ?? "", custom != null);
        if (custom != null) __result = Plugin.ToResourceId(custom);
    }
}

[HarmonyPatch(typeof(SI.UI.Image), nameof(SI.UI.Image.SetImage))]
internal static class UiImagePatch
{
    private static bool Prefix(SI.UI.Image __instance, string __0, Il2CppSystem.Object __1)
    {
        if (Plugin.LogKitRequests.Value && __0 != null && __0.StartsWith("kit2d", StringComparison.Ordinal))
        {
            var chain = new System.Text.StringBuilder();
            VisualElement e = __instance;
            for (var i = 0; i < 6 && e != null; i++, e = e.parent)
                chain.Append(e.name).Append(" < ");
            var ctx = __1 == null ? "null" : $"{__1.GetIl2CppType().FullName}: {__1.ToString()}";
            if (Plugin.LoggedKeys.Add($"img:{__0}:{chain}:{ctx}"))
                Plugin.Log.LogInfo($"[Kit2D-Img] recurso={__0} elemento={chain} contexto={ctx}");
        }
        if (!Plugin.IsCustom(__0)) return true;
        var tex = Plugin.LoadCustomTexture(__0);
        if (tex == null) return true;
        __instance.ClearRecolouring();
        __instance.tintColor = Color.white;
        __instance.image = tex;
        return false;
    }
}

[HarmonyPatch(typeof(TextElement), nameof(TextElement.text), MethodType.Setter)]
internal static class TextReplacePatch
{
    private static void Prefix(ref string value)
    {
        if (string.IsNullOrEmpty(value) || Plugin.NamePattern == null || !Plugin.NamePattern.IsMatch(value)) return;
        value = Plugin.NamePattern.Replace(value, Evaluator);
    }

    private static readonly System.Text.RegularExpressions.MatchEvaluator Evaluator = Lookup;

    private static string Lookup(System.Text.RegularExpressions.Match m) => Plugin.TextReplacements[m.Value];
}
