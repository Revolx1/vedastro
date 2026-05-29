# Estrella — астро-движок (VedAstro) + склейка. План развёртывания.

> Статус: ПЛАН (реализация по этапам с подтверждением прод-шагов).
> Продукт: Telegram-бот **Estrella** (тенант 007 в `tg_gen_bot`), голос астролога **Даниэлы Фернандес**.

---

## 0. Видение
**ИИ-астролог «чёрный пояс по всем школам».** Не одна традиция, а полимат:
- **Западная** (тропик, Placidus/Koch/Equal/Whole, аспекты, транзиты, прогрессии).
- **Ведическая/джйотиш** (сидерик, 47 аянамш, дома, накшатры, **даши** Vimshottari, дробные карты **Varga** D1–D60).
- **KP** (Krishnamurti Paddhati — есть `CalculateKP-ORI.cs`).
- **Tajik/Varshaphal** (годовые карты), **Панчанга/Мухурта** (электив времени).
- **Совместимость**: западная синастрия + ведическая **Kuta/Ashtakoot** (8 параметров).

**Принцип двух слоёв (спека §0):**
- **Слой 1 — Движок (детерминированный).** VedAstro.Library (Swiss Ephemeris, 596 расчётов). Считается математикой, воспроизводимо до бита. LLM сюда не лезет.
- **Слой 2 — Интерпретация (LLM).** Gemini в роли Даниэлы — художественный текст поверх посчитанного Слоя 1. Никогда не вычисляет астрономию и не выдумывает позиции.

---

## 1. Архитектура (3 слоя в проде)
```
Telegram → tg_gen_bot (Vercel, тенант 007: webhook, чат-UI, биллинг, i18n)
   │
   ├─ Экстрактор birth-data (Gemini Flash-Lite, NL→JSON) [спека §2]
   ├─ Геокодинг + историческая TZ (детерминированно, НЕ LLM) [§13]
   │
   ├─ VedAstro tool (lib/ai/tools/vedastro.ts) ──HTTPS+secret──▶ Астро-движок (Hetzner)
   │                                                              = ASP.NET-обёртка над VedAstro.Library
   │                                                              = наш self-host «источник» (аналог misttrack.io)
   │
   └─ Слой 2 (Gemini, Даниэла) ── интерпретирует JSON Слоя 1 ──▶ ответ + натальное колесо
                                                                  (CopilotKit action natal_chart → AstroViz, фронт ГОТОВ)
   chart_id-кэш (Redis): натал считается 1 раз/жизнь.
```

---

## 2. Инфраструктура (по факту разведки)
- **Сервер Hetzner** (Coolify-хост, `46.225.103.163`): 8 ядер · 30 ГБ RAM (свободно ~23) · 179 ГБ диск свободно · Docker 27.0.3 · Traefik 3.6.7 · ~30 контейнеров · load 0.5–1.0 (≈10%).
- **Доступ:** SSH `root@46.225.103.163` (ed25519) + Coolify MCP (apps/servers/hetzner).
- **Вывод:** ресурсов кратно достаточно, движок ставим на существующий сервер.

---

## 3. Ключевые решения
| Вопрос | Решение | Обоснование |
|---|---|---|
| **Образ движка** | **ASP.NET Minimal API обёртка над `VedAstro.Library`** (НЕ форковый `/API`) | `/API` = Azure Functions с Azure Storage/Stripe/OpenAI/Bing-обвесом (managed-сервис VedAstro). Library — чистое offline-ядро (SwissEphNet), без ключей/облака. Берём ядро, обёртку пишем сами. |
| **Школы** | Широкий доступ ко всему Calculate-набору (reflection / категорийные эндпоинты), не узкие 3 метода | «Чёрный пояс по всем школам» — LLM должен мочь запросить любую технику. |
| **Размещение** | Существующий Coolify-сервер | 8 ядер/23 ГБ свободно. |
| **Домен** | `astro-engine.giftoro.io` (Traefik + Let's Encrypt) | giftoro.io уже на сервере. |
| **Auth** | Secret-заголовок (Traefik middleware / в обёртке), т.к. движок публичный (Vercel→Hetzner) | VedAstro своей auth не имеет (только `ThrottleManager`). |
| **Лимиты** | Мягко: reservation + потолок ~6 CPU / 4 ГБ (не hard-удавка) | Расчёт короткий и кэшируется; 7 ядер простаивают. |
| **Зодиак/дома** | Тропик + Placidus по умолчанию для западных карт; сидерик+аянамша для ведических. Входят в `chart_id`. | Library поддерживает (Ayanamsa.cs; Placidus в Core.cs:3537). |
| **Лицензия** | MIT ✅ | Коммерчески чисто (спека: AGPL/GPL — хард-блок). |

---

## 4. Функционал VedAstro по школам (что доступно из Library)
| Школа / категория | Расчёты | Эндпоинт-группа обёртки |
|---|---|---|
| Западная натальная | планеты (знак/градус/дом/ретро), ASC/MC, аспекты, тропик+Placidus | `/chart/natal` |
| Транзиты/прогрессии | позиции на дату vs натал | `/chart/transits` |
| Ведическая кундали | сидерик, дома, экзальтации/дебилитации, благости | `/chart/vedic` |
| Накшатры | лунные стоянки, пады | `/nakshatra` |
| Даши | Vimshottari и др. периоды-таймлайн | `/dasha` |
| Варги (D1–D60) | Navamsa D9, Dasamsa D10 и т.д. | `/varga` |
| KP | Krishnamurti Paddhati (significators) | `/kp` |
| Панчанга/Мухурта | тити, йога, карана, электив | `/panchanga` |
| Совместимость | западная синастрия + Kuta/Ashtakoot | `/compatibility` |
| Generic | любой Calculate-метод по имени (как их API Builder) | `/calculate` (fallback) |

> Реализация: VedAstro авто-генерит API из `Calculate`-методов (см. `Data/OpenAPIStaticTable.cs`). Обёртка повторяет тот же подход (reflection над `Calculate` + категорийные обёртки для частых сценариев).

---

## 5. ЭТАП 1 — Движок на Hetzner
1. **Форк** `Revolx1/vedastro` — ✅ сделано (клон `~/vedastro`).
2. **Обёртка** — новый проект в форке (напр. `/EstrellaEngine`):
   - `Microsoft.NET.Sdk.Web`, .NET 8, ProjectReference на `../Library/Library.csproj`.
   - Minimal API: категорийные эндпоинты (§4) + `/calculate` (generic по имени метода).
   - Вход: `{date, time, tz, lat, lon, ayanamsha?, houseSystem?}` → выход: типизированный JSON.
   - Secret-middleware: проверка заголовка `X-Estrella-Secret`.
   - Health: `/healthz`.
3. **Dockerfile** (multi-stage): `sdk:8` build → `aspnet:8` runtime, `EXPOSE 8080`, скопировать Swiss Ephemeris data из Library (проверить `Library/Data` на `.se1`/`SwissEphNet`-ресурсы).
4. **Coolify-приложение**: источник = форк, Dockerfile-путь = `/EstrellaEngine/Dockerfile`, домен `astro-engine.giftoro.io`, env `ESTRELLA_ENGINE_SECRET`, лимиты мягкие, health-check `/healthz`. [прод-шаг — согласовать]
5. **DNS**: `astro-engine.giftoro.io` → IP сервера (Traefik+LE выпустит TLS). [прод-шаг]
6. **Smoke**: натальная карта `14.05.1995, 13:00, СПб` → сверить планеты/дома/ASC/MC/аспекты с эталоном (astro.com/Raphael). Проверить тропик+Placidus и ведический сидерик.

**Артефакт этапа:** `VEDASTRO_API_URL=https://astro-engine.giftoro.io`, `ESTRELLA_ENGINE_SECRET=<…>`.

---

## 6. ЭТАП 2 — Промты Слоя 2 (Даниэла-полимат)
- **System-prompt 007** (расширить `lib/ai/prompts/chat-prompts.ts`):
  - Роль: Даниэла Фернандес, владеет всеми школами; выбирает уместную технику под запрос и кратко поясняет, какой «оптикой» смотрит.
  - Жёсткое правило: **ground truth только из Слоя 1** (JSON движка), не выдумывать позиции/даты.
  - **Guardrail §6**: высокие ставки → зеркало, не директива; здоровье → специалист (без чисел/диет); кризис → выход из роли + живые ресурсы; в момент уязвимости продукт отступает, не апселлит.
  - Лимит длины (`max_tokens` — главный рычаг costs; выход в 6× дороже входа).
- **Режимы §5**: натальный ридинг (низкая темп., кэш по chart_id) / транзиты «что сейчас» / рефлексивный диалог (не кэшируется).
- Двуязычие ru/ua на всех слоях (UI уже на 12 языках; интерпретация — ru/ua приоритет).

---

## 7. ЭТАП 3 — Склейка в tg_gen_bot (точная калька MistTrack)
| MistTrack (есть) | → VedAstro (создать) |
|---|---|
| `lib/ai/tools/misttrack.ts` (клиент) | `lib/ai/tools/vedastro.ts` — `VEDASTRO_API_URL`+secret, fetch, Redis-кэш по `chart_id`, retry/backoff |
| `lib/ai/tools/misttrack-tool.ts` (`createMistrackTool`) | `lib/ai/tools/vedastro-tool.ts` (`createVedAstroTool`) — actions: natal/vedic/transits/dasha/varga/kp/panchanga/compatibility/`calculate` |
| `lib/schema/misttrack.ts` | `lib/schema/vedastro.ts` — input (birth data / chart_id / метод+параметры) |
| `lib/types/misttrack.ts` | `lib/types/vedastro.ts` |
| регистрация в `gemini-chat`/`gateway/chat` | добавить vedastro-tool в ту же точку |
| CopilotKit `misttrack_*` | **готово**: `natal_chart`/`transits`/`synastry` (`useAstroActions` + `AstroViz`) |
| — | **новое**: экстрактор birth-data (§2) + геокодинг + историческая TZ (детерминированно, перед движком) |
| Redis-кэш | `chart_id = hash(date+time+place+ayanamsha+house_system)` — натал 1 раз/жизнь |

**env tg_gen_bot (007):** `VEDASTRO_API_URL`, `ESTRELLA_ENGINE_SECRET` (Gemini-ключи уже есть).

---

## 8. Риски / митигации
- **Swiss Ephemeris data-файлы** не попадут в образ → проверить `Library` на bundled `.se1`/SwissEphNet-ресурсы; при нужде смонтировать volume.
- **CPU-пики** бьют соседей (lago-clickhouse 20%) → мягкий потолок CPU/mem + кэш (расчёт редкий).
- **Историческая TZ** (постсоветские смены зон) → отдельный детерминированный TZ-слой, не LLM.
- **Публичный движок без auth** → secret-заголовок обязателен (иначе открытый бесплатный калькулятор).
- **Reflection-generic `/calculate`** → ограничить белым списком безопасных методов (без файловых/сетевых).
- **Смешение школ в ответе** → промт Даниэлы явно выбирает одну оптику под запрос, не валит всё вместе.

---

## 9. Open items / следующий шаг
**Подтверждено:** форк ✅, домен `astro-engine.giftoro.io`, лимиты мягкие, образ = обёртка над Library, все школы.
**Следующий шаг:** написать обёртку `/EstrellaEngine` (ASP.NET Minimal API над Library) → локальный `docker build` + smoke → затем Coolify-приложение + DNS (прод, с подтверждением).
**Открыто:** (а) точные `Calculate`-сигнатуры для категорийных эндпоинтов (читаем `Logic/Calculate/Core.cs`); (б) набор дом-систем/аянамш в дефолтах; (в) глубина каталога карточек §7 под мульти-школьность.
