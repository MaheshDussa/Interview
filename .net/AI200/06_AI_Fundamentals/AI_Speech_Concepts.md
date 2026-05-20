# Introduction to AI Speech Concepts

> **One-liner**: **Speech AI = convert between spoken audio and text** — STT (speech→text), TTS (text→speech), and **Speech Translation** across languages.

---

## 1. Speech AI Capabilities

| Capability | What |
|---|---|
| **Speech-to-Text (STT)** | Transcribe audio → text |
| **Text-to-Speech (TTS)** | Synthesize natural voice from text |
| **Speech Translation** | Speak one language → text/speech in another |
| **Speaker Recognition** | Identify / verify who is speaking |
| **Keyword Recognition** | "Hey assistant" wake words |
| **Pronunciation Assessment** | Score language-learner pronunciation |
| **Real-time captioning** | Live subtitles |

---

## 2. Azure AI Speech Service

Single resource exposes:
- **Real-time STT** (WebSocket / SDK)
- **Batch STT** (transcribe many files from Blob)
- **Fast transcription API**
- **Custom Speech** (your acoustic/lexical/language models)
- **Standard / Neural / HD voices for TTS**
- **Custom Neural Voice (CNV)** (your own brand voice)
- **Speech Translation**
- **Speaker Recognition** (preview)

---

## 3. Audio Basics

| Term | Meaning |
|---|---|
| **Sample rate** | Samples per sec (16 kHz typical for speech) |
| **Bit depth** | Bits per sample (16-bit common) |
| **Channels** | Mono (1) or stereo (2) |
| **Format** | WAV (PCM), MP3, OGG, FLAC |
| **Codec** | Compression algorithm |

> Supported by Azure Speech: WAV PCM 16 kHz/16-bit mono is the baseline.

---

## 4. Speech-to-Text (STT) Modes

| Mode | Use |
|---|---|
| **Real-time** | Live captions, dictation, IVR |
| **Batch** | Transcribe many files in storage |
| **Fast transcription (sync REST)** | Quick file transcription, single call |
| **Conversation transcription** | Multi-speaker meeting, speaker diarization |

### Features
- **Profanity filtering** (mask, remove, raw).
- **Word-level timestamps**.
- **Display vs lexical** form ("two" vs "2").
- **Diarization** — who spoke when.
- **Language identification** — detect or candidate list.
- **Custom Speech** — domain words, accents, audio.

---

## 5. .NET SDK Example — Real-time STT

```csharp
// dotnet add package Microsoft.CognitiveServices.Speech

var config = SpeechConfig.FromSubscription(key, region);
config.SpeechRecognitionLanguage = "en-US";

using var audio = AudioConfig.FromDefaultMicrophoneInput();
using var rec = new SpeechRecognizer(config, audio);

rec.Recognized += (_, e) =>
{
    if (e.Result.Reason == ResultReason.RecognizedSpeech)
        Console.WriteLine($"Heard: {e.Result.Text}");
};

await rec.StartContinuousRecognitionAsync();
Console.ReadLine();
await rec.StopContinuousRecognitionAsync();
```

---

## 6. Text-to-Speech (TTS)

| Voice type | Quality | Notes |
|---|---|---|
| **Standard** | Robotic | Retired |
| **Neural** | Natural | Default today |
| **Neural HD** | Highest, expressive | Newer voices |
| **Multilingual neural** | One voice across many langs | Cross-language |
| **Custom Neural Voice (CNV)** | Your brand voice | Requires consent + approval |

### Output formats
- WAV / MP3 / OGG / WebM
- 8/16/24/48 kHz
- 16/24-bit

---

## 7. SSML — Speech Synthesis Markup Language

XML to control prosody, voice, style, emphasis.

```xml
<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis"
       xml:lang="en-US">
  <voice name="en-US-AvaNeural">
    <mstts:express-as style="cheerful" xmlns:mstts="https://www.w3.org/2001/mstts">
      Welcome <emphasis level="strong">back</emphasis>!
    </mstts:express-as>
    <break time="400ms"/>
    Your balance is <say-as interpret-as="cardinal">1024</say-as> dollars.
  </voice>
</speak>
```

Common tags: `<voice>`, `<prosody rate pitch volume>`, `<break>`, `<say-as>`, `<phoneme>`, `<emphasis>`, `<mstts:express-as style>`.

---

## 8. TTS .NET Example

```csharp
var cfg = SpeechConfig.FromSubscription(key, region);
cfg.SpeechSynthesisVoiceName = "en-US-AvaNeural";
cfg.SetSpeechSynthesisOutputFormat(
    SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);

using var synth = new SpeechSynthesizer(cfg, AudioConfig.FromWavFileOutput("out.wav"));
await synth.SpeakTextAsync("Hello world!");
```

For style/SSML: `await synth.SpeakSsmlAsync(ssml);`.

---

## 9. Speech Translation

```csharp
var cfg = SpeechTranslationConfig.FromSubscription(key, region);
cfg.SpeechRecognitionLanguage = "en-US";
cfg.AddTargetLanguage("es");
cfg.AddTargetLanguage("fr");

using var rec = new TranslationRecognizer(cfg);
rec.Recognized += (_, e) => {
    foreach (var t in e.Result.Translations)
        Console.WriteLine($"{t.Key}: {t.Value}");
};
await rec.StartContinuousRecognitionAsync();
```

Pipeline internally: STT (source) → MT → optional TTS (target).

---

## 10. Custom Speech

Train custom models when:
- Domain vocabulary (medical, legal, parts numbers).
- Accents not handled well.
- Noisy environment recordings.

### Inputs you can supply
| Data | Boosts |
|---|---|
| **Plain text** | Word frequency / new vocab |
| **Pronunciation file** | Word → phonemes |
| **Structured text (lexical)** | Grammar |
| **Audio + transcript** | Acoustic + lexical |
| **Audio only** | Acoustic (limited) |

Workflow: upload data → train model → test/evaluate (WER) → deploy → endpoint.

### Metric
- **WER (Word Error Rate)** = (Substitutions + Insertions + Deletions) / total reference words. Lower is better.

---

## 11. Custom Neural Voice (CNV)

- Record ~300+ utterances from a single voice talent.
- Requires **written consent** + Microsoft approval (Responsible AI gate).
- Trained model produces synthetic voice indistinguishable from talent.
- Two tiers: **Lite** (smaller dataset) and **Pro**.

---

## 12. Speaker Recognition (preview)

| Mode | What |
|---|---|
| **Verification** | "Is this Alice?" (1:1) |
| **Identification** | "Who is speaking?" (1:N) |
| **Text-dependent** | Fixed passphrase |
| **Text-independent** | Free speech |

Stores **voice profiles** — treat as **biometric data** with strict consent/retention rules.

---

## 13. Pronunciation Assessment

- For language learners.
- Scores: **accuracy, fluency, completeness, pronunciation score** per phoneme, word, sentence.
- Used in apps like learning English.

---

## 14. Real-time Architecture Pattern

```
Mic → WebSocket → Speech SDK → STT → Bot/LLM → TTS → Speaker
        ↑ partial results streamed for low latency
```

For a phone IVR:
```
Telephony → Azure Communication Services → Speech (STT/TTS)
                                         → Dialog logic → Speech back
```

---

## 15. Security & Compliance

| Concern | Approach |
|---|---|
| Voice data is biometric | Encrypt at rest, restrict access, get consent |
| PII in transcripts | Run **PII detection** (Language service) before storage |
| MI auth to Speech | Use Entra token instead of subscription key |
| Region & data residency | Pick region; data stays in region by default |
| Custom voice misuse | CNV requires Responsible AI approval + watermark |

---

## 16. Common Pitfalls

| Pitfall | Fix |
|---|---|
| 8 kHz telephony audio with 16 kHz model | Use telephony-tuned model or upsample |
| Poor WER on jargon | Add **phrase list** or train Custom Speech |
| Choppy TTS | Stream chunks; use SDK streaming events |
| Wrong voice gender for SSML | Set `voice name` (e.g., `en-US-JennyNeural`) |
| Mixing keys + MI | Pick one; prefer MI |
| Diarization missing | Enable conversation transcription mode |
| Latency on first audio | Pre-warm connection / use persistent WebSocket |

---

## 17. Interview / Exam Q&A

**Q1. Difference between STT and TTS?**
STT converts audio to text; TTS generates audio from text.

**Q2. What is SSML?**
Speech Synthesis Markup Language — XML controlling voice, prosody, style, pronunciation in TTS.

**Q3. WER?**
Word Error Rate — accuracy metric for STT. Lower is better.

**Q4. When to use Custom Speech?**
When domain vocabulary, accents, or audio conditions aren't handled by the base model.

**Q5. Neural vs Standard voices?**
Neural voices use deep learning for natural prosody; Standard voices are legacy/robotic and being retired.

**Q6. Speaker verification vs identification?**
Verification = 1:1 (is this person X?). Identification = 1:N (which person?).

**Q7. How does speech translation work internally?**
STT in source → MT → optional TTS in target language.

**Q8. Real-time vs batch transcription?**
Real-time uses streaming WebSocket for low-latency partial results. Batch processes files from Blob asynchronously.

**Q9. Why does Custom Neural Voice require approval?**
To prevent impersonation/abuse — Microsoft requires Responsible AI gating and explicit voice talent consent.

**Q10. How to reduce false wake-ups for keyword recognition?**
Use a custom keyword model trained on your phrase + on-device confidence threshold.

---

## 18. CLI / REST Cheat Sheet

```powershell
# List voices
$key = "<key>"; $region = "eastus"
Invoke-RestMethod -Uri "https://$region.tts.speech.microsoft.com/cognitiveservices/voices/list" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key }

# Fast transcription
$body = @{
  contentUrls = @("https://blob/example.wav")
  locales = @("en-US")
  profanityFilterMode = "Masked"
}
Invoke-RestMethod -Method Post `
  -Uri "https://$region.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key; "Content-Type"="application/json" } `
  -Body ($body | ConvertTo-Json)
```

---

## 19. Mental Model

> **Speech AI = audio ⇄ text + voice cloning + translation. Pick real-time vs batch, neural voices for quality, SSML for control, Custom Speech for domain vocabulary, and treat voice data as biometric PII.**
