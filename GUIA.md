# Guia FM26: camisas, nomes e faces grátis

Com BepInEx, um plugin pequeno e o Real Names Fix, dá para trocar nomes abreviados (VDG → Vasco da Gama), colocar camisas próprias em 2D e 3D e reaproveitar facepacks — tudo de graça, inclusive em save já em andamento.

---

## O que você consegue com este guia

Cinco recursos, todos gratuitos. Só os nomes dos clubes brasileiros pelo Real Names Fix exigem save novo; o plugin resolve o save atual.

| Recurso | Como | Save em andamento? |
| --- | --- | --- |
| Nomes reais de clubes e competições | Real Names Fix (arquivos `.lnc`/`.dbc`) | Sim, menos clubes brasileiros |
| Siglas brasileiras (VDG, SEP, CEC…) → nome completo | Plugin, arquivo `nomes.txt` | Sim |
| Faces de jogadores | Facepack na pasta `graphics/faces` | Sim |
| Camisa 3D na partida | Plugin, PNG 1024×1024 por ID do time | Sim |
| Camisa 2D nas telas | Plugin, PNG 256×256 por assinatura | Sim |

Nada aqui altera o executável nem os arquivos protegidos do jogo: o plugin troca nomes e imagens na hora em que o jogo os pede, e tudo volta ao normal se você remover a pasta dele.

---

## Requisitos

Você precisa do BepInEx 6 (versão IL2CPP) já funcionando no FM26; o resto só é necessário para compilar o plugin e gerar camisas.

| Ferramenta | Para quê | Observação |
| --- | --- | --- |
| [BepInEx 6 IL2CPP](https://github.com/BepInEx/BepInEx/releases) (testado no be.755) | Carregar o plugin | Rode o jogo uma vez após instalar para gerar `BepInEx/interop` |
| [.NET SDK 8](https://dotnet.microsoft.com/download) | Compilar o plugin | O plugin é net6.0; o SDK 8 compila |
| Python 3 + Pillow + UnityPy | Extrair moldes do jogo e gerar camisas | `pip install pillow UnityPy` |

**Pastas usadas neste guia:**

| Nome | Windows (Steam) | Linux (Proton) |
| --- | --- | --- |
| `<JOGO>` | `C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26` | `~/.steam/steam/steamapps/common/Football Manager 26` |
| `<USUARIO>` | `Documentos\Sports Interactive\Football Manager 26` | `~/.steam/steam/steamapps/compatdata/3551340/pfx/drive_c/users/steamuser/Documents/Sports Interactive/Football Manager 26` |

No Linux, se a Steam estiver em outro lugar, troque `~/.steam/steam` por `~/.steam/debian-installation`. O `3551340` é o AppID do FM26.

> **Antes de começar:** faça backup da pasta `<USUARIO>/games` (seus saves).

---

## Passo 1 — Nomes reais (Real Names Fix)

O jogo atual usa a base `<JOGO>/shared/data/database/db/2620` (versão 26.2+). Aplique a fix nela — aplicar só na `2600`, como muita gente faz, não tem efeito.

1. Feche o FM26.
2. Baixe o pack no [FMScout](https://www.fmscout.com/a-fm26-real-names-license-fix.html) e copie `FM26 Fix by FMScout.lnc` e `FM26 Club Names by FMScout.lnc` para `db/2620/lnc/all`.
3. Em `db/2620/lnc/all`, apague: `ac milan (wom).lnc`, `acc Inter unlic 24.lnc`, `acmilan unlic 24.lnc`, `atalanta u23 (men).lnc`, `fake.lnc`, `inter (wom).lnc`, `inter u23 (men).lnc`, `inter unlic 24.lnc`, `japanese names.lnc`, `lazio (wom).lnc`, `lic_dan_swe_fra.lnc`, `licensing club name_fm24.lnc`, `men lazio.lnc`, `men.atalanta.lnc`.
4. Em `db/2620/edt/permanent`, apague `fake.edt`.
5. Em `db/2620/dbc/permanent`, apague: `1_japan_removed_clubs.dbc`, `brazil_kits.dbc`, `forbidden names.dbc`, `japan_fake.dbc`, `japan_unlicensed_random_names.dbc`, `j league non player.dbc`, `licensing26.dbc`.
6. Em `db/2620/dbc/permanent/language`, apague `Licensing2.dbc` e `Licensing2_chn.dbc`.

O `brazil_kits.dbc` é o arquivo que força as siglas de 3 letras nos clubes brasileiros, mas ele só vale para **save novo**. Para o save atual, use o `nomes.txt` do Passo 3.

> Atualizações do jogo e "Verificar integridade dos arquivos" na Steam podem trazer esses arquivos de volta: se as siglas voltarem, aplique de novo.

---

## Passo 2 — Faces dos jogadores

O FM26 lê facepacks no mesmo formato dos FMs anteriores, então um pack do FM24 funciona: os IDs dos jogadores se mantêm entre as versões.

1. Copie a pasta `faces` do pack (com o `config.xml` dentro) para `<USUARIO>/graphics/faces`.
2. O `config.xml` deve mapear cada imagem para `graphics/pictures/person/<ID>/portrait`, igual aos packs antigos.
3. Reinicie o jogo. A primeira abertura demora alguns minutos enquanto ele indexa as imagens (com 46 mil faces, uns 4 minutos lendo o disco).

**Dica no Linux:** se o pack já está na pasta do FM24 e o disco está cheio, use hard link em vez de copiar — não ocupa espaço extra:

```bash
cp -al "<USUARIO_FM24>/graphics/faces" "<USUARIO>/graphics/faces"
```

Jogadores que surgiram depois do pack e os regens continuam com a silhueta; só um facepack mais novo resolve.

---

## Passo 3 — Plugin FM26CustomKits

O plugin fica em `<JOGO>/BepInEx/plugins/FM26CustomKits/` com esta estrutura:

```
FM26CustomKits/
├── FM26CustomKits.dll
├── nomes.txt        ← siglas → nomes
├── kits3d/          ← <ID>_home.png, <ID>_away.png, <ID>_third.png, <ID>_gk.png (1024×1024)
└── kits2d/          ← <assinatura>.png (256×256, fundo transparente)
```

### Compilar do fonte

1. Clone este repositório ou baixe `Plugin.cs` e `FM26CustomKits.csproj`.
2. Ajuste `GameDir` no `.csproj` para a sua `<JOGO>`.
3. Rode `dotnet build -c Release`. O `.csproj` já copia a DLL e cria as pastas no lugar certo.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>FM26CustomKits</AssemblyName>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <GameDir>CAMINHO\Football Manager 26</GameDir>
    <BepInExDir>$(GameDir)/BepInEx</BepInExDir>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="$(BepInExDir)/core/BepInEx.Core.dll" Private="false" />
    <Reference Include="$(BepInExDir)/core/BepInEx.Unity.IL2CPP.dll" Private="false" />
    <Reference Include="$(BepInExDir)/core/0Harmony.dll" Private="false" />
    <Reference Include="$(BepInExDir)/core/Il2CppInterop.Runtime.dll" Private="false" />
    <Reference Include="$(BepInExDir)/core/Il2CppInterop.Common.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/Il2Cppmscorlib.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/UnityEngine.CoreModule.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/UnityEngine.ImageConversionModule.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/UnityEngine.UIElementsModule.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/FM.Graphics.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/FM.Match.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/FM.UI.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/SI.Match.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/SI.UI.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/SI.Bindable.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/SI.Bindable.Reference.Core.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/SI.Core.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/Unity.ResourceManager.dll" Private="false" />
    <Reference Include="$(BepInExDir)/interop/Unity.Addressables.dll" Private="false" />
  </ItemGroup>
  <Target Name="Deploy" AfterTargets="Build">
    <MakeDir Directories="$(BepInExDir)/plugins/FM26CustomKits/kits3d;$(BepInExDir)/plugins/FM26CustomKits/kits2d" />
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(BepInExDir)/plugins/FM26CustomKits" />
  </Target>
</Project>
```

### nomes.txt

Uma troca por linha. A sigla é trocada como palavra inteira, inclusive dentro de notícias e da agenda. Exemplo para a Série A:

```
VDG=Vasco da Gama
ATM=Atlético-MG
ATP=Athletico-PR
BAH=Bahia
BOT=Botafogo
CBR=CRB
CEC=Cruzeiro
COR=Corinthians
CTB=Coritiba
FLA=Flamengo
FLU=Fluminense
GOI=Goiás
GRE=Grêmio
INT=Internacional
MFC=Mirassol
SAN=Santos
SEP=Palmeiras
SPO=São Paulo
VIT=Vitória
CEA=Ceará
CUI=Cuiabá
FOR=Fortaleza
```

> **Atenção:** Não coloque `BRA` (Bragantino): é a mesma sigla da nacionalidade Brasil e trocaria a nacionalidade de todo jogador brasileiro. Confira as siglas na tabela do seu save antes — algumas mudam entre bases de dados.

### Ativar log em tempo real

Em `<JOGO>/BepInEx/config/BepInEx.cfg`, mude `InstantFlushing = false` para `true`. Sem isso o log só é gravado no disco depois, e você não vê as linhas do Passo 4 com o jogo aberto.

---

## Passo 4 — Descobrir o ID do time e as assinaturas

A camisa 3D é escolhida pelo **ID do time**; a 2D, por uma **assinatura** que o plugin calcula, porque nas telas o jogo não informa o ID — só o modelo e as cores da camisa genérica.

### ID do time (3D)

- Com o Real Names Fix aplicado, a página do clube mostra o ID ao lado do nome — o Vasco aparece como `Vasco da Gama (ID: 339)`.
- Outra forma: no pack de escudos, o arquivo do clube é `club_<ID>.png`.
- Ou abra uma partida do time e procure no `<JOGO>/BepInEx/LogOutput.log`: `[Kit] time=339 uniforme=home …`.

Salve as camisas como `kits3d/339_home.png`, `kits3d/339_away.png` e `kits3d/339_third.png`.

### Assinatura (2D)

1. Abra a página do clube (Clube → Visão Geral), onde aparecem a camisa titular e a reserva.
2. Rode o script `assinaturas.py` (detecta o log automaticamente):

```bash
python scripts/assinaturas.py
```

Exemplo de saída:

```
Tipo                                Assinatura   Uniforme   Recurso
────────────────────────────────────────────────────────────────────
frente (tela do clube)              fde7df98     home       kit2d_1
frente (tela do clube)              c720460b     away       kit2d_1
linha (tática/escalação)            077f109c                kit2d_outfield_same_1
goleiro (tática/escalação)          3a1b8f22                kit2d_outfield_same_1
```

3. `frente` + `home`/`away` são as camisas da página do clube; `linha` é a camisa da tela de táticas; `goleiro` é a do goleiro. Abra só a página do seu time antes de rodar o script para não misturar assinaturas de outros clubes.
4. Salve a imagem como `kits2d/<assinatura>.png` — por exemplo `kits2d/fde7df98.png`. Não precisa reiniciar: saia e volte na tela.

As assinaturas da sua base podem ser diferentes das do exemplo: use sempre as do seu log. Se precisar exportar para uma planilha: `python scripts/assinaturas.py --csv > assinaturas.csv`.

---

## Passo 5 — Criar as camisas

A forma mais rápida: peça a arte a uma IA de imagem (ChatGPT ou similar) e monte a textura 3D em cima de um molde extraído do próprio jogo.

### Formatos que o jogo aceita

| Tipo | Tamanho | Formato |
| --- | --- | --- |
| 3D (partida) | 1024×1024 PNG | Layout das camisas licenciadas: frente embaixo no centro, gola acima dela, costas no alto do centro, mangas nas laterais, calção nos cantos de baixo |
| 2D (telas) | 256×256 PNG | Camisa de frente, centralizada, fundo transparente |

### Extrair um molde 3D do jogo

As camisas licenciadas ficam em pacotes Unity em `<JOGO>/fm_Data/StreamingAssets/LicensedData/Core` e `/Extra`. Use o script `extrair_moldes.py` para extraí-las de uma vez:

```bash
# Todos os países (pode demorar)
python scripts/extrair_moldes.py "<JOGO>" --saida moldes/

# Só clubes brasileiros e argentinos
python scripts/extrair_moldes.py "<JOGO>" --paises bra,arg --saida moldes/
```

Isso cria uma pasta `moldes/` com todos os PNGs 1024×1024 (3D) e 256×256 (2D) das camisas licenciadas do jogo. Use-os só como referência visual de posição para desenhar a sua. **Não redistribua as texturas licenciadas do jogo.**

Para a 2D, os arquivos `kit2d_generic_*.png` têm 56 modelos genéricos em que o canal vermelho é a cor principal, o verde a secundária e o azul gola e punhos.

### Prompts para IA de imagem

Peça estampas separadas, não uma prancha pronta — a IA não consegue seguir o layout 3D do jogo, mas estampas contínuas encaixam em qualquer região:

```
Seamless tileable texture, 1024x1024, flat and front-facing, no perspective, no shadows, no fabric folds.
[descreva a estampa: cor de fundo, desenho em tom sobre tom].
No text, no logos, no watermark. The texture must tile seamlessly on all four edges.
```

Para a camisa 2D, peça a camisa pronta com fundo transparente:

```
Single football jersey, FRONT VIEW ONLY, flat product render, perfectly centered, square 1:1,
TRANSPARENT background (PNG with alpha), no mannequin, no person, no text, no numbers.
Short sleeves, round crew neck. [descreva o design: cor, faixa, gola, escudo/cruz, logo do fornecedor].
```

Depois recorte a sobra e redimensione para 256×256. A direção das faixas importa: "do ombro esquerdo do jogador ao quadril direito" é, para quem olha, do alto à direita para baixo à esquerda — escreva as duas formas no prompt.

### Montar as camisas com `gerar_camisa.py`

Com as artes geradas, o script monta os arquivos finais no formato certo para o plugin.

**Camisa 2D** — pega a imagem de frente e centraliza no canvas 256×256:

```bash
python scripts/gerar_camisa.py 2d --frente arte_frente.png --saida kits/
```

**Camisa 3D** — encaixa cada parte no layout UV 1024×1024 do jogo:

```bash
python scripts/gerar_camisa.py 3d \
  --frente  arte_frente.png \
  --costas  arte_costas.png \
  --manga   arte_manga.png  \
  --calcao  arte_calcao.png \
  --saida   kits/
```

`--costas`, `--manga` e `--calcao` são opcionais: se omitidos, essas regiões ficam transparentes no PNG e o jogo usa a cor genérica do time nessa área. O mínimo viável é só `--frente`.

Depois renomeie os arquivos gerados:
- 3D → `kits3d/<ID>_home.png` (use o ID do time do Passo 4)
- 2D → `kits2d/<assinatura>.png` (use a assinatura do `assinaturas.py`)

---

## Como funciona por dentro

O FM26 é dividido em duas partes: a simulação roda no motor nativo `game_plugin.dll` (criptografado e protegido — o plugin não mexe nele) e a interface e os gráficos rodam em C# (Unity IL2CPP), onde o BepInEx com Harmony consegue interceptar métodos.

| Método do jogo | O que o jogo faz | O que o plugin faz |
| --- | --- | --- |
| `KitMaterials.TryGetLicensedKitTextureResourceID(teamID, kitType)` | Pergunta se o time tem camisa licenciada | Se existe `kits3d/<ID>_<tipo>.png`, responde "sim" com um ID próprio |
| `MatchCharacterFactory.TryGetKitTexture(resourceID)` | Carrega a textura da camisa licenciada | Para o ID próprio, carrega o PNG do disco |
| `DatabaseObjectImageHelpers.ConvertDynamicReferenceToAddressableID` | Escolhe a imagem 2D (modelo + cores) | Calcula a assinatura e, se existe o PNG, devolve o ID próprio |
| `SI.UI.Image.SetImage(id)` | Mostra uma imagem na tela | Para o ID próprio, coloca o PNG direto no elemento |
| `TextElement.text` (setter) | Escreve qualquer texto na tela | Troca as siglas do `nomes.txt` |
| `GraphicsUtils.ResolveKitResourceID(teamID, kitType)` | Resolve qual recurso de camisa usar para o time | Registra o caminho do PNG customizado em `PendingCustomKit` para uso no próximo patch |
| `KitMaterials.ApplyGenericKitToTextureArray(kitSetup, …, array, slot, …)` | Desenha a camisa genérica (cor sólida) no slot do `Texture2DArray`; apenas para times sem licença | Intercepta: se existe PNG para este time, chama `ApplyLicensedKitWithNumberToTextureArray` com nossa textura. É o mecanismo principal da camisa 3D em clubes sem licença |

Por que não usar a pasta `graphics/kits` como nos FMs antigos: o FM26 até procura `graphics/pictures/team/<ID>/kits/home` e `.../kit_textures/home`, mas para clubes sem licença ele desenha a camisa genérica direto pelas cores e nunca chega a olhar a pasta. Com o plugin, o clube passa a ser tratado como licenciado.

---

## Limitações conhecidas e problemas comuns

| Problema | Causa | O que fazer |
| --- | --- | --- |
| O jogo parece travado na primeira abertura | Está indexando o facepack | Espere: com dezenas de milhares de faces leva alguns minutos |
| Camisa nova não aparece após trocar o PNG | O plugin guarda a textura na memória | Reinicie o jogo |
| Tela de táticas continua com a camisa antiga | A tela guarda as imagens em cache | Reinicie o jogo |
| Outro clube aparece com a sua camisa nas telas | A assinatura 2D depende só do modelo e das cores genéricas | Raro; só acontece se o outro clube tiver exatamente as mesmas cores. Na partida 3D não ocorre (vai por ID) |
| Siglas voltaram | Atualização da Steam restaurou os arquivos do Passo 1 | Reaplique o Passo 1 |
| Nada aparece no log | `InstantFlushing = false` | Ative no `BepInEx.cfg` (Passo 3) |

O que este método **não** faz: mudar dinheiro, atributos ou qualquer dado da simulação (isso fica no motor protegido; use o In-Game Editor oficial), nem faixas e bandeirões de torcida (o motor do FM26 não tem esse recurso). Testado no FM26 com BepInEx 6.0.0-be.755, rodando via Proton no Linux; no Windows o caminho é o mesmo, só mudam as pastas.

---

## Código-fonte e scripts

| Arquivo | O que é |
| --- | --- |
| [`Plugin.cs`](Plugin.cs) | Código do plugin BepInEx/Harmony |
| [`scripts/extrair_moldes.py`](scripts/extrair_moldes.py) | Extrai templates de camisas dos bundles do jogo |
| [`scripts/gerar_camisa.py`](scripts/gerar_camisa.py) | Monta 2D e 3D a partir de arte gerada em IA |
| [`scripts/assinaturas.py`](scripts/assinaturas.py) | Lê o log do BepInEx e lista as assinaturas 2D |

Licença MIT — use e modifique à vontade.

Os logs `[Kit]` e `[Kit2D]` podem ser desligados em `BepInEx/config/fm26.community.customkits.cfg` (`LogKitRequests = false`) depois que você tiver os IDs e assinaturas.

---

## Fontes

- [FMScout — FM26 Real Names License Fix](https://www.fmscout.com/a-fm26-real-names-license-fix.html) (lista de arquivos e aviso sobre clubes brasileiros)
- [Trivela — Football Manager 26: clubes brasileiros](https://trivela.com.br/games/football-manager-26-clubes-brasileiros-uptades-elencos/)
- [FM Graphics — FM26 Graphics Installation](https://footballmanagergraphics.com/topic/3104-fm26-graphics-installation)
- [BepInEx — releases](https://github.com/BepInEx/BepInEx/releases)
- [UnityPy](https://github.com/K0lb3/UnityPy) (extração das texturas)
