"""
Monta camisas 3D (1024×1024) e 2D (256×256) a partir de arte gerada em IA.

──────────────────────────────────────────────────────────────────────────────
MODO 2D  —  centraliza a imagem de frente num canvas 256×256

  python gerar_camisa.py 2d --frente arte_frente.png --saida kits/

  A imagem deve ter fundo transparente. O script recorta a área opaca,
  redimensiona para caber em 244×244 e centraliza no canvas 256×256.

──────────────────────────────────────────────────────────────────────────────
MODO 3D  —  encaixa cada parte no layout UV 1024×1024 do FM26

  python gerar_camisa.py 3d \\
    --frente  arte_frente.png \\
    --costas  arte_costas.png \\      (opcional; usa frente se omitido)
    --manga   arte_manga.png \\       (opcional; preenche ambas as mangas)
    --calcao  arte_calcao.png \\      (opcional; preenche ambos os calções)
    --saida   kits/

  Layout do UV 1024×1024:
  ┌─────────────┬──────────────┬─────────────┐
  │ manga esq.  │   COSTAS     │ manga dir.  │
  │ (10-329)    │  (340-679)   │ (680-1019)  │  y: 100-579
  ├─────────────┼──────────────┼─────────────┤
  │  calção e.  │   FRENTE     │  calção d.  │
  │ (10-334)    │  (340-679)   │ (680-1019)  │  y: 600-1023
  └─────────────┴──────────────┴─────────────┘

──────────────────────────────────────────────────────────────────────────────
NOMEAR OS ARQUIVOS DE SAÍDA

  3D → renomeie para  kits3d/<ID_DO_TIME>_home.png
  2D → renomeie para  kits2d/<ASSINATURA>.png
       (use assinaturas.py para descobrir a assinatura)

──────────────────────────────────────────────────────────────────────────────
Dependência:
  pip install pillow
"""

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    sys.exit("Instale Pillow:  pip install pillow")


# ── Coordenadas do layout UV 1024×1024 do FM26 ───────────────────────────────
# Detectadas a partir do template unlicensed extraído do jogo.
REGIOES = {
    "frente":   (340, 600, 679, 1019),
    "costas":   (340, 100, 679,  574),
    "manga_e":  ( 10, 100, 329,  579),
    "manga_d":  (680, 100, 1019, 579),
    "calcao_e": ( 10, 760, 334, 1023),
    "calcao_d": (680, 760, 1019, 1023),
}


def carregar(path: str) -> Image.Image:
    return Image.open(path).convert("RGBA")


def recortar_opaco(img: Image.Image) -> Image.Image:
    """Remove bordas totalmente transparentes."""
    alpha = img.getchannel("A").point(lambda v: 255 if v > 16 else 0)
    bbox = alpha.getbbox()
    return img.crop(bbox) if bbox else img


def encaixar(canvas: Image.Image, arte: Image.Image, regiao: tuple,
             espelhar_h: bool = False, espelhar_v: bool = False) -> None:
    """Redimensiona a arte para caber na região e cola no canvas."""
    x0, y0, x1, y1 = regiao
    rw, rh = x1 - x0, y1 - y0

    # Redimensionar mantendo proporção, preenchendo a região
    scale = max(rw / arte.width, rh / arte.height)
    nw = round(arte.width * scale)
    nh = round(arte.height * scale)
    arte = arte.resize((nw, nh), Image.LANCZOS)

    if espelhar_h:
        arte = arte.transpose(Image.FLIP_LEFT_RIGHT)
    if espelhar_v:
        arte = arte.transpose(Image.FLIP_TOP_BOTTOM)

    # Centralizar dentro da região
    ox = x0 + (rw - nw) // 2
    oy = y0 + (rh - nh) // 2
    canvas.alpha_composite(arte, (ox, oy))


# ── Modo 2D ───────────────────────────────────────────────────────────────────

def build_2d(frente_path: str) -> Image.Image:
    arte = recortar_opaco(carregar(frente_path))
    escala = 244 / max(arte.width, arte.height)
    arte = arte.resize(
        (round(arte.width * escala), round(arte.height * escala)),
        Image.LANCZOS,
    )
    out = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    out.alpha_composite(arte, ((256 - arte.width) // 2, (256 - arte.height) // 2))
    return out


# ── Modo 3D ───────────────────────────────────────────────────────────────────

def build_3d(frente_path: str, costas_path: str = None,
             manga_path: str = None, calcao_path: str = None) -> Image.Image:
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))

    frente = recortar_opaco(carregar(frente_path))
    costas = recortar_opaco(carregar(costas_path)) if costas_path else frente

    # Frente e costas no centro
    encaixar(canvas, frente,  REGIOES["frente"])
    encaixar(canvas, costas,  REGIOES["costas"])

    # Mangas (espelhadas entre si)
    if manga_path:
        manga = recortar_opaco(carregar(manga_path))
        encaixar(canvas, manga, REGIOES["manga_e"])
        encaixar(canvas, manga, REGIOES["manga_d"], espelhar_h=True)

    # Calções (mesma arte, espelhada)
    if calcao_path:
        calcao = recortar_opaco(carregar(calcao_path))
        encaixar(canvas, calcao, REGIOES["calcao_e"])
        encaixar(canvas, calcao, REGIOES["calcao_d"], espelhar_h=True)

    return canvas


# ── CLI ───────────────────────────────────────────────────────────────────────

def cmd_2d(args):
    saida = Path(args.saida)
    saida.mkdir(parents=True, exist_ok=True)

    out = build_2d(args.frente)
    nome = Path(args.frente).stem + "_2d_256.png"
    dest = saida / nome
    out.save(dest)
    print(f"2D: {dest}")
    print("→ renomeie para  kits2d/<assinatura>.png")


def cmd_3d(args):
    saida = Path(args.saida)
    saida.mkdir(parents=True, exist_ok=True)

    out = build_3d(
        frente_path=args.frente,
        costas_path=getattr(args, "costas", None),
        manga_path=getattr(args, "manga", None),
        calcao_path=getattr(args, "calcao", None),
    )
    nome = Path(args.frente).stem + "_3d_1024.png"
    dest = saida / nome
    out.save(dest)
    print(f"3D: {dest}")
    print("→ renomeie para  kits3d/<ID_DO_TIME>_home.png")


def main():
    ap = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    sub = ap.add_subparsers(dest="modo", required=True)

    # 2D
    p2 = sub.add_parser("2d", help="Montar camisa 2D (256×256)")
    p2.add_argument("--frente", required=True, metavar="PNG",
                    help="Imagem de frente da camisa (fundo transparente)")
    p2.add_argument("--saida", default="kits", metavar="DIR")

    # 3D
    p3 = sub.add_parser("3d", help="Montar camisa 3D (1024×1024)")
    p3.add_argument("--frente",  required=True, metavar="PNG",
                    help="Frente da camisa")
    p3.add_argument("--costas",  metavar="PNG",
                    help="Costas da camisa (padrão: mesma que frente)")
    p3.add_argument("--manga",   metavar="PNG",
                    help="Manga (usada em ambos os lados, direita é espelhada)")
    p3.add_argument("--calcao",  metavar="PNG",
                    help="Calção (usado nos dois lados, direito é espelhado)")
    p3.add_argument("--saida", default="kits", metavar="DIR")

    args = ap.parse_args()
    {"2d": cmd_2d, "3d": cmd_3d}[args.modo](args)


if __name__ == "__main__":
    main()
