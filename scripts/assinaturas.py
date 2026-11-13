"""
Lê o log do BepInEx e lista as assinaturas 2D de cada camisa visualizada no jogo.

Uso:
  python assinaturas.py                        # detecta o log automaticamente
  python assinaturas.py <LOG>                  # caminho explícito
  python assinaturas.py --csv > resultado.csv  # exportar como CSV

O log é gerado em:
  Windows: <JOGO>/BepInEx/LogOutput.log
  Linux:   ~/.steam/steam/steamapps/common/Football Manager 26/BepInEx/LogOutput.log

Pré-requisito: abrir a tela do clube no jogo com o plugin instalado.
Se nada aparecer, ative InstantFlushing=true em BepInEx/config/BepInEx.cfg.
"""

import argparse
import os
import re
import sys
from pathlib import Path

LOG_LINE = re.compile(
    r"\[Kit2D\]\s+"
    r"assinatura=(?P<sig>[0-9a-f]+)\s+"
    r"tipo=(?P<tipo>\S+)\s+"
    r"uniforme=(?P<uni>\S*)\s+"
    r"recurso=(?P<rec>\S+)\s+"
    r"cores=(?P<cores>.*)"
)

TIPO_LABEL = {
    "Kit2DFront": "frente (tela do clube)",
    "Kit2DOutfield": "linha (tática/escalação)",
    "Kit2DGK": "goleiro (tática/escalação)",
}


def auto_log():
    candidatos = []

    if os.name == "nt":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE,
                                r"SOFTWARE\Wow6432Node\Valve\Steam") as k:
                steam = Path(winreg.QueryValueEx(k, "InstallPath")[0])
            candidatos.append(
                steam / "steamapps/common/Football Manager 26/BepInEx/LogOutput.log"
            )
        except Exception:
            pass

    for base in [
        Path.home() / ".steam/steam",
        Path.home() / ".steam/debian-installation",
    ]:
        candidatos.append(
            base / "steamapps/common/Football Manager 26/BepInEx/LogOutput.log"
        )

    for p in candidatos:
        if p.exists():
            return p
    return None


def parse_log(path: Path):
    rows = []
    seen = set()
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        m = LOG_LINE.search(line)
        if not m:
            continue
        key = (m["sig"], m["tipo"], m["uni"])
        if key in seen:
            continue
        seen.add(key)
        rows.append(m.groupdict())
    return rows


def main():
    ap = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    ap.add_argument("log", nargs="?", metavar="LOG",
                    help="Caminho para LogOutput.log (padrão: detecta automaticamente)")
    ap.add_argument("--csv", action="store_true", help="Saída em CSV")
    args = ap.parse_args()

    if args.log:
        log_path = Path(args.log).expanduser()
        if not log_path.exists():
            sys.exit(f"Arquivo não encontrado: {log_path}")
    else:
        log_path = auto_log()
        if log_path is None:
            sys.exit(
                "Log não encontrado automaticamente.\n"
                "Informe o caminho: python assinaturas.py <LOG>"
            )
        if not args.csv:
            print(f"Log: {log_path}\n")

    rows = parse_log(log_path)

    if not rows:
        sys.exit(
            "Nenhuma linha [Kit2D] encontrada no log.\n\n"
            "Checklist:\n"
            "  1. O plugin FM26CustomKits.dll está em BepInEx/plugins/FM26CustomKits/\n"
            "  2. Você abriu a tela do clube no jogo (Clube → Visão Geral)\n"
            "  3. InstantFlushing = true em BepInEx/config/BepInEx.cfg\n"
            "  4. O log correto é <JOGO>/BepInEx/LogOutput.log (não BepInEx.log)"
        )

    if args.csv:
        print("assinatura,tipo,uniforme,recurso,cores")
        for r in rows:
            cores = r["cores"].replace(",", ";")
            print(f"{r['sig']},{r['tipo']},{r['uni']},{r['rec']},{cores}")
        return

    print(f"{'Tipo':<35} {'Assinatura':<12} {'Uniforme':<10} Recurso")
    print("─" * 80)
    for r in rows:
        label = TIPO_LABEL.get(r["tipo"], r["tipo"])
        uni = r["uni"] or "—"
        print(f"{label:<35} {r['sig']:<12} {uni:<10} {r['rec']}")

    print(f"\n{len(rows)} assinatura(s) encontrada(s)")
    print()
    print("Salve cada camisa em:  kits2d/<assinatura>.png  (256×256, fundo transparente)")
    print()
    print("Exemplo:")
    if rows:
        ex = rows[0]
        uni = ex["uni"] or "home"
        print(f"  Camisa {uni} do time:  kits2d/{ex['sig']}.png")


if __name__ == "__main__":
    main()
