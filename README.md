# MacroHaxBall

Macro global de teclado para [HaxBall](https://www.haxball.com/): aperte **`;`** (ponto e vírgula) e o sistema dispara **2–3× `X`** (a tecla de chute). Segurando o gatilho, o disparo continua na taxa máxima (modo hold).

Escrito em **C# (.NET 10)** com hooks nativos do Windows (`WH_KEYBOARD_LL` + `SendInput`). Não injeta nada no jogo — simula teclas reais no nível do sistema, então o navegador as enxerga como entrada de usuário legítima.

## Download

Baixe o exe pronto em [Releases](https://github.com/KauaBR0/haxball-macro/releases) (`MacroHaxBall.exe`, Windows x64, single-file self-contained — não precisa de .NET instalado).

## Uso

1. Rode `MacroHaxBall.exe` (cria `config.json` ao lado na primeira execução)
2. Foque o navegador com o HaxBall e aperte **`;`** (ponto e vírgula) → dispara 2–3× `X`
3. **Segure `;`** para disparo contínuo na taxa máxima
4. **`F8`** pausa/retoma (útil para digitar a tecla do gatilho de verdade no chat)
5. **`Ctrl+C`** encerra

## Configuração (`config.json`)

| Campo | Padrão | Descrição |
|---|---|---|
| `TriggerKey` | `Oem2` | Tecla gatilho (nomes do enum `Vk`: `Oem2` = tecla do `;` ponto e vírgula; outros: `X`, `F8`, …) |
| `TriggerMatch` | `Auto` | Como casar o gatilho: `Vk` (por tecla virtual), `ScanCode` (por tecla física) ou `Auto` (qualquer dos dois — cobre ABNT2) |
| `TriggerScanCode` | `0x35` | Scan code físico usado nos modos `ScanCode`/`Auto` |
| `ToggleKey` | `F8` | Liga/desliga o macro |
| `FireKey` | `X` | Tecla disparada pelo macro |
| `ConsumeTrigger` | `true` | Bloqueia a tecla do gatilho de chegar ao jogo (não abre chat/comando) |
| `RepeatWhileHeld` | `true` | Segurar o gatilho dispara continuamente (modo hold) |
| `UseRandomCount` | `true` | `true` = 2–3 aleatório (`MinCount`/`MaxCount`); `false` = `FixedCount` |
| `MinCount` / `MaxCount` | `2` / `3` | Faixa da contagem aleatória |
| `FixedCount` | `3` | Contagem fixa quando `UseRandomCount: false` |
| `PressMs` | `15` | Duração de cada pressionamento (ms) |
| `InterKeyDelayMs` | `40` | Intervalo entre pressionamentos (ms) — aumenta se quiser disparo mais lento |
| `AllowInjected` | `false` | Aceita teclas sintéticas como gatilho (para automação/testes) |
| `Verbose` | `false` | Loga toda tecla pressionada (debug: descubra o vk/scan da sua tecla) |

### Teclado brasileiro (ABNT2)

O gatilho padrão (`TriggerKey: Oem2` / scan `0x35`) é a tecla do **ponto e vírgula (`;`, com `:` no Shift)**, ao lado do Shift direito.

- **Aperte `;`** para disparar o macro; segure para disparo contínuo.
- O símbolo `/` impresso no teclado ABNT2 fica em outra posição física e **não dispara o macro** — não é bug.
- Para trocar de tecla, ative `"Verbose": true`, reinicie e aperte a tecla desejada: o console mostra `down <nome> (vk 0x.., scan 0x..)` — use o valor em `TriggerKey` (modo `Vk`) ou `TriggerScanCode` (modo `ScanCode`).
- Se ainda assim nada disparar, confira o `TriggerMatch`: `Auto` casa por tecla virtual **ou** scan code — o mais robusto.

## Build

Requer [.NET SDK 10+](https://dotnet.microsoft.com/download).

```bash
dotnet publish MacroHaxBall -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist
```

## Como funciona

```
teclado físico / injetado
        │  WH_KEYBOARD_LL (global, thread com message pump)
        ▼
Program: detecta gatilho → consome o ";" → enfileira burst
        ▼
MacroWorker (thread dedicada, serializada)
        ▼
KeySender: SendInput × N (down/hold/up)  ← nunca bloqueia o callback do hook
        ▼
navegador (HaxBall) recebe X, X, X como entrada real
```

- O callback do hook **nunca** executa sleeps nem `SendInput` (o Windows remove hooks lentos) — o trabalho vai para o `MacroWorker` via fila.
- Teclas injetadas são ignoradas por padrão (anti-reentrada); repetição automática do gatilho é consumida durante o hold.
- Pausa via `F8` também interrompe o loop do modo hold.

## Estrutura

```
MacroHaxBall/
├── Program.cs          # lógica do gatilho, toggle e modo hold
├── KeyboardHook.cs     # WH_KEYBOARD_LL em thread com message pump
├── MacroWorker.cs      # fila serializada (sleeps/SendInput fora do hook)
├── KeySender.cs        # SendInput (down/hold/up, burst)
├── MacroConfig.cs      # carregamento/validação do config.json
├── NativeMethods.cs    # P/Invoke Win32
├── Vk.cs               # códigos de teclas virtuais
└── KeyEventInfo.cs     # evento de teclado + flag de consumo
tools/                  # scripts de teste (injeção de tecla, captura)
plan.md                 # plano original do projeto
```

## Aviso

Macro simula digitação client-side; em salas que proíbem macros, seu uso é responsabilidade de cada jogador.
