# FM26 Custom Kits

> Plugin BepInEx/Harmony para Football Manager 26 que injeta camisas customizadas na renderização 3D das partidas e nas telas 2D da UI — sem modificar nenhum arquivo do jogo.

**[Guia completo em português →](GUIA.md)**

---

## Por que existe

A pasta `graphics/kits/` só funciona para clubes licenciados no FM26. Para clubes brasileiros sem licença, o jogo bloqueia o carregamento de camisas personalizadas em arquivos internos da engine.

Existem duas formas de contornar isso:

| Abordagem | Como funciona | Funciona em save existente? | Modifica arquivos do jogo? |
| --- | --- | --- | --- |
| **Deletar arquivos de bloqueio** (ex: BrasilMundiUp) | Remove os arquivos que impedem o carregamento → `graphics/kits/` passa a funcionar | ❌ Save novo obrigatório | ✅ Sim |
| **Este plugin** | Intercepta os métodos da engine em tempo de execução via Harmony | ✅ Funciona em saves existentes | ❌ Não |

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

**Para descobrir o ID do time:** entre numa partida com o time. O plugin atualiza automaticamente o arquivo `referencia.txt` na pasta do plugin:

```
FM26CustomKits/referencia.txt

## CAMISAS 3D
ID            tipo      status
------------------------------------------------------
339           home      sem camisa
339           away      sem camisa
```

### Camisas 2D (UI)

A camisa 2D usa uma **assinatura** baseada nas cores do time, não no ID. Para descobrir a assinatura:

1. Abra a tela do clube no jogo
2. O plugin registra a assinatura automaticamente em `referencia.txt`:

```
## ASSINATURAS 2D
assinatura  tipo                             uniforme  status
------------------------------------------------------------------------
a3f52b01    frente (tela do clube)           home      sem camisa
c8e91a44    linha  (tática/escalação)        home      sem camisa
```

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

Packs distribuídos no formato `.fmf` (FMScout, etc.) são ZIPs renomeados. Para usar:

1. Renomeie o `.fmf` para `.zip` e extraia
2. Localize os PNGs de camisa dentro do arquivo
3. Renomeie para o formato `<ID>_home.png` e coloque em `kits3d/`

> **BrasilMundiUp** usa uma abordagem diferente: deleta arquivos de bloqueio do jogo para que `graphics/kits/` funcione com clubes brasileiros. Os packs deles **não são diretamente compatíveis** com este plugin sem renomear os arquivos.

## Licença

MIT — código aberto, use e modifique à vontade.
