# Prompts para gerar camisas com IA

Use estes prompts no **ChatGPT**, **DALL-E**, **Midjourney** ou qualquer gerador de imagem.
Gere cada parte separadamente e monte com `gerar_camisa.py`.

---

## O que gerar para cada modo

| Peça | Usado em | Dimensão ideal |
| --- | --- | --- |
| Frente da camisa | 2D (obrigatório) + 3D | Quadrado, qualquer res |
| Costas da camisa | 3D (opcional) | Quadrado, qualquer res |
| Manga | 3D (opcional) | Retângulo vertical |
| Calção | 3D (opcional) | Retângulo |

Se omitir costas, manga ou calção no modo 3D, o jogo usa a cor genérica do time nessas regiões — resultado perfeitamente aceitável.

---

## Prompt base — frente da camisa (2D e 3D)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background (PNG with alpha channel), no mannequin, no person,
no shadows, no fabric wrinkles, square 1:1 format.
Short sleeves, round crew neck.
[DESCREVA O DESIGN AQUI]
```

**Substituir `[DESCREVA O DESIGN AQUI]` por uma descrição como:**

| Design | Exemplo de descrição |
| --- | --- |
| Cor sólida | `Solid black jersey with white collar and white cuffs.` |
| Faixa diagonal | `White jersey with a wide diagonal black sash from the right shoulder to the left hip. Black collar and cuffs.` |
| Listras verticais | `Red and black vertical stripes, equal width. Black collar.` |
| Listras horizontais | `Light blue and white horizontal stripes. White collar.` |
| Ombros coloridos | `White jersey with black shoulder panels and black collar.` |
| Gradiente | `Jersey with a gradient from deep blue at the top to black at the bottom.` |

> **Dica de faixa:** a IA às vezes inverte a direção. Descreva assim para garantir:
> `diagonal black sash going from the upper-right shoulder area to the lower-left hip (viewer's perspective)`

---

## Prompt base — costas da camisa (3D)

```
Football jersey, BACK VIEW, flat product photo, perfectly centered,
transparent background (PNG with alpha channel), no mannequin, no person,
no shadows, no fabric wrinkles, square 1:1 format.
Short sleeves, round crew neck. No numbers, no name on back.
[MESMO DESIGN DA FRENTE]
```

> Na maioria das vezes a frente e costas são idênticas; omitir `--costas` e deixar o script repetir a frente costuma funcionar bem.

---

## Prompt base — manga (3D)

```
Football jersey sleeve, isolated on transparent background,
vertical orientation, flat product photo, no shadows.
[DESCREVA A COR E DETALHES DA MANGA]
```

Exemplo:
```
Football jersey sleeve, isolated on transparent background,
vertical orientation, flat product photo, no shadows.
Black sleeve with two thin white stripes near the cuff.
```

---

## Prompt base — calção (3D)

```
Football shorts, front view, flat product photo, perfectly centered,
transparent background (PNG with alpha channel), no person,
no shadows, square 1:1 format.
[DESCREVA A COR E DETALHES DO CALÇÃO]
```

---

## Exemplos prontos por estilo de clubes brasileiros

### Faixa diagonal (estilo Vasco, Fluminense)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background (PNG with alpha channel), no mannequin, no shadows, square 1:1.
Short sleeves, round crew neck.
Black jersey with a wide diagonal white sash going from the upper-right
shoulder to the lower-left hip (viewer's perspective). White collar and cuffs.
```

### Listras verticais finas (estilo Santos, Botafogo alternativo)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background, no mannequin, no shadows, square 1:1.
Short sleeves, round crew neck.
White jersey with thin vertical black pinstripes evenly spaced.
Solid black collar and black sleeve cuffs.
```

### Manto (cor sólida + detalhes)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background, no mannequin, no shadows, square 1:1.
Short sleeves, round crew neck.
Deep red jersey (wine/maroon tone). White V-neck collar with thin gold trim.
White cuffs with thin gold stripe.
```

### Camisa com ombros contrastantes (estilo Atlético-MG)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background, no mannequin, no shadows, square 1:1.
Short sleeves, round crew neck.
Black jersey with white vertical stripes only on the front center panel.
Black sleeves and sides. Black collar.
```

### Estampado / textura especial (estilo terceiro uniforme moderno)

```
Football jersey, FRONT VIEW, flat product photo, perfectly centered,
transparent background, no mannequin, no shadows, square 1:1.
Short sleeves, round crew neck.
Dark navy blue jersey with a subtle geometric diamond pattern in slightly
lighter blue tone-on-tone. Gold collar and gold cuffs.
```

---

## Dicas gerais

**Fundo transparente:** sempre peça `transparent background (PNG with alpha channel)`. Se a IA entregar fundo branco, recorte no Photoshop ou use `gerar_camisa.py` que remove automaticamente bordas brancas opacas.

**Logos e escudos:** não peça o escudo real do clube no prompt — a IA vai errar ou distorcer. Gere a camisa sem escudo e adicione o PNG do escudo separadamente com `--escudo escudo.png` no `gerar_camisa.py 3d`.

**Número de tentativas:** gere de 4 a 8 variações e escolha a melhor. Pequenas diferenças no prompt mudam muito o resultado.

**Proporções:** qualquer tamanho quadrado serve; o script redimensiona automaticamente. Peça pelo menos 512×512 para qualidade melhor no 3D.

**Iteração:** se a primeira tentativa ficou próxima do ideal mas com um detalhe errado, reenvie a imagem para a IA com `Edit this image: [correção específica]` em vez de gerar do zero.
