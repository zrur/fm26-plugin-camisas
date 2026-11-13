"""
Extrai moldes de camisas dos bundles Unity do FM26.

Uso:
  python extrair_moldes.py <JOGO_DIR> [--saida DIR] [--paises bra,arg,...]

Exemplos:
  # Windows (Steam)
  python extrair_moldes.py "C:/Program Files (x86)/Steam/steamapps/common/Football Manager 26"

  # Linux (Proton)
  python extrair_moldes.py ~/.steam/steam/steamapps/common/"Football Manager 26"

  # Só clubes brasileiros e uruguaios, pasta de saída personalizada
  python extrair_moldes.py ~/fm26 --paises bra,uru --saida meus_moldes/

Dependência:
  pip install UnityPy
"""

import argparse
import sys
from pathlib import Path

try:
    import UnityPy
except ImportError:
    sys.exit("Instale UnityPy:  pip install UnityPy")


def find_bundles(game_dir: Path, paises: list) -> list:
    bundles = []
    for subdir in ("Core", "Extra"):
        base = game_dir / "fm_Data" / "StreamingAssets" / "LicensedData" / subdir
        if not base.exists():
            continue
        for f in sorted(base.iterdir()):
            if f.suffix != ".bundle" or "kit" not in f.name.lower():
                continue
            if paises and not any(p in f.name for p in paises):
                continue
            bundles.append(f)
    return bundles


def extract_bundle(bundle: Path, saida: Path, verbose: bool) -> int:
    env = UnityPy.load(str(bundle))
    count = 0
    for obj in env.objects:
        if obj.type.name != "Texture2D":
            continue
        tex = obj.read()
        if not tex.m_Name:
            continue
        dest = saida / f"{tex.m_Name}.png"
        if dest.exists():
            if verbose:
                print(f"  já existe: {dest.name}")
            continue
        tex.image.save(dest)
        if verbose:
            print(f"  {dest.name}  ({tex.m_Width}×{tex.m_Height})")
        count += 1
    return count


def main():
    ap = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    ap.add_argument("jogo", metavar="JOGO_DIR", help="Pasta raiz do Football Manager 26")
    ap.add_argument("--saida", default="moldes", metavar="DIR", help="Pasta de saída (padrão: moldes/)")
    ap.add_argument(
        "--paises",
        default="",
        metavar="bra,arg,...",
        help="Filtrar por país (vazio = todos). Ex: bra,uru,arg",
    )
    ap.add_argument("--quiet", action="store_true", help="Menos output")
    args = ap.parse_args()

    game_dir = Path(args.jogo).expanduser()
    if not game_dir.exists():
        sys.exit(f"Pasta não encontrada: {game_dir}")

    saida = Path(args.saida)
    saida.mkdir(parents=True, exist_ok=True)

    paises = [p.strip() for p in args.paises.split(",") if p.strip()]
    bundles = find_bundles(game_dir, paises)

    if not bundles:
        sys.exit(
            "Nenhum bundle de camisas encontrado.\n"
            "Verifique o caminho do jogo e se a pasta fm_Data/StreamingAssets/LicensedData existe."
        )

    total = 0
    for bundle in bundles:
        if not args.quiet:
            print(f"Extraindo {bundle.name}…")
        n = extract_bundle(bundle, saida, not args.quiet)
        total += n
        if not args.quiet and n == 0:
            print("  (nenhum arquivo novo)")

    print(f"\n{total} imagem(ns) extraída(s) → {saida}/")

    # Dicas de uso
    genericos = list(saida.glob("*generic*")) + list(saida.glob("*kit2d*default*"))
    tridim = list(saida.glob("*kit3d*")) or list(saida.glob("*home*"))
    if not args.quiet:
        print()
        if genericos:
            print(f"Moldes 2D genéricos disponíveis: {', '.join(p.name for p in genericos[:5])}")
        if tridim:
            print(f"Exemplos de moldes 3D: {', '.join(p.name for p in tridim[:3])}")
        print("\nUse gerar_camisa.py --molde3d <arquivo_3d> --molde2d <arquivo_2d> para gerar suas camisas.")


if __name__ == "__main__":
    main()
