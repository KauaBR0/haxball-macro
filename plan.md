# Plano — Macro HaxBall: `/` → 2–3× `X`

## 1. Objetivo

Macro de teclado global: ao pressionar **`/`**, o sistema envia automaticamente **2 ou 3 pressionamentos de `X`** (quantidade aleatória ou fixa, configurável), com pequeno intervalo entre eles. **Segurando o gatilho pressionado, o disparo continua na taxa máxima (modo hold).** O objetivo é executar múltiplos chutes rápidos no HaxBall (que roda no navegador).

Não é injeção no jogo: simula teclas reais via API do Windows, então o navegador enxerga entrada de usuário legítima (`isTrusted = true`).

## 2. Decisão de stack: **C# (.NET)**

Ambiente verificado: SDK .NET `10.0.300` já instalado; compilador C++ (`cl`) ausente no PATH.

| Critério | C# (.NET) | C++ (Win32/MSVC) |
|---|---|---|
| Devimento (hook + SendInput) | Rápido, P/Invoke direto | Mais boilerplate (WndProc, header, build) |
| Facilidade de manter | Alta | Média |
| Latência | ~1 ms — irrelevante para macro | ~microsegundos — irrelevante |
| Build/instalação | `dotnet publish` single-file self-contained | Precisaria instalar Build Tools primeiro |
| Risco de bug de memória (hook global) | Baixo (managed) | Alto (ponteiro de callback) |

**Conclusão:** C#. Latência não é fator aqui; produtividade e segurança do hook global favorecem C#. Mesmas APIs do Windows à disposição via P/Invoke (`SetWindowsHookEx`, `SendInput`).

## 3. Arquitetura

Aplicativo console .NET, um processo residente em bandeja/vazio no terminal:

```
┌─────────────────────────────────────────────┐
│  Program.cs — entrypoint, carrega config    │
│  KeyboardHook.cs — WH_KEYBOARD_LL global    │
│      │  detecta keydown de "/" (ignora      │
│      │  repeat e teclas injetadas)          │
│      ▼                                      │
│  TriggerHandler — consome o "/" (opcional)  │
│      ▼                                      │
│  KeySender.cs — SendInput: 2–3× X down/up  │
│      com delay entre teclas                 │
└─────────────────────────────────────────────┘
       │
       ▼
  Navegador (HaxBall) recebe X, X, X
```

### Componentes

- **KeyboardHook** (`SetWindowsHookEx(WH_KEYBOARD_LL)`)
  - Padrão: funciona com o foco em qualquer janela (HaxBall roda no navegador).
  - Callback filtra: `LLKHF_INJECTED` (não reagir a teclas sintéticas), repetição automática (`WM_KEYDOWN` repetido), e `key up`.
  - **Correspondência por `vkCode` configurável** (default `VK_OEM_2` = tecla `/`). Se der problema com layout ABNT2, trocar match para *scan code* (físico, independente de layout).
- **TriggerHandler** — decisões configuráveis:
  - `ConsumeTrigger: true` (default): devolve `1` no hook, bloqueando o `/` de chegar ao navegador.
  - Toggle por tecla (default `F8`): pausa/retoma o macro — útil para digitar `/` de verdade no chat.
- **KeySender** (`SendInput`)
  - Para cada disparo: `KEYDOWN` + delay (`InterKeyDelayMs`, default ~30 ms) + `KEYUP` — enfileira os 2–3 X sem travar a fila do navegador.
  - Quantidade: `UseRandomCount: true` → `Random(2..3)`; senão `FixedCount`.
- **Config** — `config.json` ao lado do exe (tecla gatilho, quantidade, delays, toggle, consume).

## 4. Etapas (milestones)

| # | Tarefa | Critério de aceite |
|---|---|---|
| 1 | `dotnet new console` → projeto `MacroHaxBall` (net10.0-windows) | Compila e roda no terminal |
| 2 | `KeyboardHook` WH_KEYBOARD_LL com evento de keydown + filtros (repeat/injected/up) | Log imprimindo as teclas pressionadas |
| 3 | `KeySender` — `SendInput` de 2–3 `X` com delay | Teste no Bloco de Notas: foca, aperta `/`, aparecem 2–3 `x` |
| 4 | TriggerHandler: consumir `/`, toggle `F8`, contagem aleatória | Bloco de Notas: `/` não aparece; `F8` desliga/liga |
| 5 | `config.json` + carga/validação | Mudar `FixedCount` no JSON surte efeito sem recompilar |
| 6 | Teste real no HaxBall (navegador focado, dentro de uma sala) | Apertar `/` dispara os chutes; sem abrir chat |
| 7 | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` | Exe único executável em qualquer máquina Win11 |

## 5. Riscos e mitigações

- **Layout de teclado (ABNT2)**: posição do `/` varia. Mitigação: `vkCode` configurável primeiro; fallback scan-code no código.
- **`/` abrindo chat/comandos do jogo**: resolvido por `ConsumeTrigger` (bloqueio no hook). Se o usuário quiser o `/` passando, basta config.
- **Macro como vantagem injusta**: é simulação client-side, indistinguível de digitação rápida. Sem anti-cheat no HaxBall; uso em salas que proíbem macro é responsabilidade do usuário.
- **Hook vazando/erro**: processo C# morre sozinho se o hook falhar (desinstala hook em `finally`); sem trava de sistema.
- **Foco em outra janela**: por ser hook global, o macro dispara em qualquer app — o toggle `F8` cobre esse caso.

## 6. Fora de escopo (por enquanto)

- GUI/tray icon sofisticado, autostart no Windows, múltiplos perfis de macro.
- Disparo em janela específica (só no navegador) — exige WinEventHook de foreground; adicionar só se pedido.

## 7. Próximo passo

Com o plano aprovado: criar o projeto em `MacroHaxBall/` e implementar os milestones 1–5, depois testar ao vivo no HaxBall (milestone 6).