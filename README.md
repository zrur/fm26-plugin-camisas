# FM26 Custom Kits

> Plugin BepInEx/Harmony para Football Manager 26 que injeta camisas customizadas na renderização 3D das partidas e nas telas 2D da UI — sem modificar nenhum arquivo do jogo.

**[Guia completo em português →](GUIA.md)**

---

## Por que existe

A pasta `graphics/kits/` continua funcionando no FM26 — mas só para os times que o jogo já reconhece como licenciados. Para a maioria dos clubes brasileiros (sem licença), o FM26 ignora essa pasta e desenha a camisa pelas cores genéricas, nunca chegando a verificar se existe um PNG lá. Este plugin intercepta os métodos internos da engine para contornar exatamente isso, fazendo o jogo tratar qualquer time como se tivesse camisa licenciada.

Diferente dos megapacks tradicionais (que chegam a vários GB para cobrir todos os clubes do mundo), aqui você coloca **só as camisas dos times que você quer** — cada PNG pesa menos de 1 MB. O plugin carrega apenas o que está na pasta; o resto continua usando as cores genéricas do jogo.

## Como funciona

O plugin usa Harmony para interceptar métodos internos do FM26:

| Método interceptado | O que faz |
| --- | --- |
| `GraphicsUtils.ResolveKitResourceID` | Identifica qual time está sendo renderizado |
| `KitMaterials.ApplyGenericKitToTextureArray` | Substitui a camisa genérica pelo PNG customizado |
| `KitMaterials.TryGetLicensedKitTextureResourceID` | Faz o jogo "acreditar" que o time tem camisa licenciada |
| `DatabaseObjectImageHelpers.ConvertDynamicReferenceToAddressableID` | Substitui a camisa 2D nas telas de UI |
| `SI.UI.Image.SetImage` | Renderiza o PNG customizado no elemento da UI |
| `TextElement.text` (setter) | Substitui siglas por nomes completos (ex: VDG → Vasco da Gama) |

## Requisitos

- Football Manager 26 (Steam, Windows ou Linux)
- [BepInEx 6 Bleeding Edge IL2CPP x64](https://builds.bepinex.dev/projects/bepinex_be)
- .NET SDK 8+ (apenas para compilar do fonte)

## Instalação

**1. Instale o BepInEx 6 IL2CPP** na pasta raiz do FM26 e abra o jogo uma vez para gerar os arquivos de configuração.

**2. Baixe o release** e extraia para `<FM26>/BepInEx/plugins/`.

**3. Abra o jogo.** O plugin cria as pastas automaticamente no primeiro boot:

```
<FM26>/BepInEx/plugins/FM26CustomKits/
├── FM26CustomKits.dll
├── nomes.txt          ← substituição de nomes de clubes
├── kits3d/            ← camisas 3D (partida)
└── kits2d/            ← camisas 2D (telas de UI)
```

## Adicionando camisas

### Camisas 3D (partida)

Coloque PNGs em `kits3d/` com o nome `<ID>_<tipo>.png`:

```
kits3d/
├── 339_home.png       ← camisa titular do time 339
├── 339_away.png       ← camisa visitante
├── 339_third.png      ← terceiro uniforme
└── 339_gk.png         ← goleiro
```

**Dimensões recomendadas:** 1024×1024 px

**Para descobrir o ID do time:** ative `LogKitRequests = true` no arquivo de configuração, entre numa partida com o time e procure no log:

```
BepInEx/LogOutput.log → [Kit] ResolveKitResourceID time=339 uniforme=home
```

### Camisas 2D (UI)

A camisa 2D usa uma **assinatura** baseada nas cores do time, não no ID. Para descobrir a assinatura:

1. Abra a tela do clube no jogo
2. Rode `python scripts/assinaturas.py` — detecta o log automaticamente e lista todas as assinaturas
3. Salve o PNG como `kits2d/<assinatura>.png`

**Dimensões recomendadas:** 256×256 px com fundo transparente

### Substituição de nomes

Edite `nomes.txt` para substituir siglas por nomes completos em toda a UI:

```
# SIGLA=Nome exibido
VDG=Vasco da Gama
BOT=Botafogo de Futebol e Regatas
```

## Scripts Python

O repositório inclui scripts para extrair moldes do jogo, montar as camisas a partir de arte gerada em IA e descobrir assinaturas 2D.

```bash
pip install -r requirements.txt

# Extrair templates de camisas dos bundles do jogo
python scripts/extrair_moldes.py "<JOGO>" --saida moldes/

# Montar camisa 2D (256×256) a partir de arte gerada em IA
python scripts/gerar_camisa.py 2d --frente arte_frente.png --saida kits/

# Montar camisa 3D (1024×1024) a partir de arte gerada em IA
python scripts/gerar_camisa.py 3d --frente arte_frente.png --costas arte_costas.png --saida kits/

# Listar assinaturas 2D dos times visitados no jogo
python scripts/assinaturas.py
```

Veja [`scripts/prompts_ia.md`](scripts/prompts_ia.md) para prompts prontos de direção artística para cada estilo de camisa.

## Compilando do fonte

```bash
~/.dotnet/dotnet build -c Release
# Saída: bin/Release/net6.0/FM26CustomKits.dll
```

## Compatibilidade com packs existentes

Packs distribuídos no formato `.fmf` (Brasil Mundiup, FMScout, etc.) são ZIPs renomeados. Para usar:

1. Renomeie o `.fmf` para `.zip` e extraia
2. Localize os PNGs de camisa dentro do arquivo
3. Renomeie para o formato `<ID>_home.png` e coloque em `kits3d/`

## Licença

MIT — código aberto, use e modifique à vontade.
