# Introduction to Natural Language Processing (NLP)

> **One-liner**: **NLP = teach computers to read, understand, and write human language.**

---

## 1. What is NLP?

NLP is the field of AI focused on **text and spoken language**. Tasks include:

- Detect language
- Find names, places, dates (NER)
- Classify intent / sentiment / topic
- Extract key phrases
- Summarize / translate / answer questions
- Generate text (LLMs)

**Analogy**: Teaching a non-native speaker English — first letters, then words, then meaning, then nuance.

---

## 2. The NLP Pipeline (classical)

```
Raw text
  → Tokenization (split into tokens)
  → Normalization (lowercase, strip punctuation)
  → Stop-word removal ("the", "is")
  → Stemming / Lemmatization ("running" → "run")
  → Vectorization (BoW, TF-IDF, embeddings)
  → Model (classifier, NER, etc.)
```

| Step | Example |
|---|---|
| **Tokenize** | "I love AI." → ["I","love","AI","."] |
| **Lemmatize** | "better" → "good" |
| **Stem** | "studies" → "studi" (crude) |
| **TF-IDF** | Term frequency × inverse doc frequency |

---

## 3. From Bag-of-Words to Embeddings

| Approach | Idea | Loses |
|---|---|---|
| **One-hot** | 1 in slot for word | Meaning, similarity |
| **Bag of Words (BoW)** | Word counts | Order, context |
| **TF-IDF** | Weighted counts | Order, context |
| **Word2Vec / GloVe** | Static word embeddings | Polysemy (1 vector per word) |
| **Contextual embeddings (BERT, GPT)** | Same word, different vector by context | — best quality |

---

## 4. Transformers — the modern NLP backbone

- Architecture from 2017 paper "Attention is All You Need".
- Core mechanism: **self-attention** (each token attends to every other).
- Two families:
  - **Encoder-only** (BERT) → classification, NER, embeddings.
  - **Decoder-only** (GPT) → generation.
  - **Encoder-Decoder** (T5, BART) → translation, summarization.

---

## 5. Common NLP Tasks

| Task | Example | Output |
|---|---|---|
| **Language detection** | "Bonjour" → French | Lang code |
| **Sentiment analysis** | "Great service!" → positive (0.95) | Label + score |
| **Key phrase extraction** | Find topical phrases | List of phrases |
| **NER** (Named Entity Recognition) | "Satya leads MS" → Person, Org | Entities + types |
| **Entity linking** | Link "Apple" → Apple Inc. (KB id) | KB URI |
| **PII detection** | Find SSN, emails | Redacted text |
| **Summarization** | Long → short | Abstractive or extractive |
| **Translation** | EN → ES | Translated text |
| **Question Answering** | Doc + Q → A | Span or answer |
| **Intent classification** | "Book a flight" → BookFlight | Intent label |
| **Topic modeling** | Cluster documents | Topic vectors |

---

## 6. Azure AI Language Service — features

| Feature | What |
|---|---|
| **Language detection** | Detect language of text |
| **Sentiment + opinion mining** | Positive/neutral/negative + per-aspect |
| **Key phrase extraction** | Topic phrases |
| **NER (general)** | People, places, organizations, dates |
| **PII detection** | Find/redact sensitive info |
| **Entity linking** | Resolve to Wikipedia |
| **Text analytics for health** | Medical entities & relations |
| **Custom NER** | Train on your entities |
| **Custom text classification** | Single/multi-label |
| **Summarization** | Abstractive (LLM-style) + extractive |
| **Conversational Language Understanding (CLU)** | Intents & entities for bots |
| **Question Answering** | Build Q&A from FAQ / docs |
| **Custom translation** | (via Translator) |

---

## 7. .NET SDK Example — `Azure.AI.TextAnalytics`

```csharp
// dotnet add package Azure.AI.TextAnalytics

var client = new TextAnalyticsClient(
    new Uri("https://<resource>.cognitiveservices.azure.com/"),
    new DefaultAzureCredential());

string text = "Microsoft was founded by Bill Gates. Great company!";

var lang = await client.DetectLanguageAsync(text);
Console.WriteLine($"Lang: {lang.Value.Name}");

var sentiment = await client.AnalyzeSentimentAsync(text);
Console.WriteLine($"Sentiment: {sentiment.Value.Sentiment}");

var entities = await client.RecognizeEntitiesAsync(text);
foreach (var e in entities.Value)
    Console.WriteLine($"{e.Text} ({e.Category})");

var pii = await client.RecognizePiiEntitiesAsync("My SSN is 123-45-6789");
Console.WriteLine(pii.Value.RedactedText);
```

---

## 8. Conversational Language Understanding (CLU)

Replaces the older **LUIS**. Builds intent + entity models for chatbots.

```
Utterance:   "Book a flight to Paris tomorrow"
Intent:      BookFlight
Entities:    destination=Paris, date=tomorrow
```

Workflow: define schema → label utterances → train → deploy → call from bot.

---

## 9. Custom Question Answering

- Build a Q&A knowledge base from FAQ pages, PDFs, or pairs.
- Hosted as a project inside Language service.
- Supports synonyms, multi-turn (follow-up prompts), chit-chat.

---

## 10. Translator Service

- 100+ languages, neural MT.
- **Document Translation** (preserves Word/PDF formatting).
- **Custom Translator** — train on your bilingual data for domain-specific quality.
- Endpoint: `https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=es`.

```csharp
// REST POST with header Ocp-Apim-Subscription-Key and JSON body [{ "Text": "Hello" }]
```

---

## 11. Tokenization & Languages

- Whitespace tokenizers fail for Chinese/Japanese/Thai — use language-specific or sub-word (BPE).
- **Sub-word tokenization** (BPE, WordPiece, SentencePiece) handles unknown words by breaking them down.
- Pre-trained multilingual models (XLM-R, mBERT, GPT-4o) handle 100+ langs.

---

## 12. Evaluation Metrics

| Task | Metric |
|---|---|
| Classification (intent, sentiment) | Accuracy, F1, confusion matrix |
| NER | Precision/Recall/F1 per entity type |
| Summarization | ROUGE-1/2/L |
| Translation | BLEU, chrF, COMET |
| QA | Exact match (EM), F1 |
| Embedding | NDCG, MRR for retrieval |

---

## 13. Bias & Safety

- Train data bias → biased predictions.
- Use **PII detection** to avoid leaking personal data.
- Use **Content Safety** to filter hate/violence/sexual/self-harm.
- Consider language coverage — many languages are low-resource.

---

## 14. Architecture Patterns

### Bot with CLU
```
User → Bot Framework → CLU (intent+entities)
         ↓
         Logic → DB / API → Response
```

### RAG over your docs
```
User Q → Embed → Azure AI Search → Top chunks
       → Azure OpenAI (with citations) → Answer
```

### Customer feedback analytics
```
Reviews → Language API (sentiment + key phrases + opinion mining)
       → Power BI dashboard
```

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Sending raw PII to model | Run **PII detection** first; redact |
| Using LUIS in new projects | Migrate to **CLU** |
| Ignoring language | Detect first; pick appropriate model |
| Confusing extractive vs abstractive summary | Extractive = picks sentences; abstractive = rewrites |
| Custom NER on tiny dataset | Need 10-50+ examples per entity |
| Slow batch | Use **batch endpoints** (analyze) for many docs |
| Wrong vector dim in search | Embedding model & index must match |

---

## 16. Interview / Exam Q&A

**Q1. What is tokenization?**
Splitting text into smaller units (tokens) — words or sub-words — that the model can process.

**Q2. Difference between stemming and lemmatization?**
Stemming = crude chop ("studies" → "studi"). Lemmatization = dictionary form ("studies" → "study").

**Q3. BoW vs embedding?**
BoW counts words ignoring order; embeddings encode meaning as dense vectors and capture similarity.

**Q4. What does NER do?**
Identifies named entities (Person, Org, Location, Date) in text.

**Q5. Extractive vs abstractive summarization?**
Extractive picks salient sentences. Abstractive generates new sentences capturing meaning.

**Q6. CLU vs LUIS?**
CLU is the newer service in Azure AI Language; LUIS is being retired.

**Q7. When to use Custom NER?**
When you need to recognize domain-specific entities (e.g., part numbers, drug names) the prebuilt model doesn't know.

**Q8. How does sentiment + opinion mining differ?**
Sentiment = overall doc/sentence polarity. Opinion mining = per-aspect (target + assessment), e.g., "battery: poor".

**Q9. PII detection — common use?**
Detect & redact SSNs, emails, phones before storing or sending to an LLM.

**Q10. What's a transformer's "attention"?**
Mechanism letting each token weigh and combine information from all other tokens, producing contextual representations.

---

## 17. CLI / REST Cheat Sheet

```powershell
# Detect language (REST)
$body = @{ documents = @(@{ id="1"; text="Bonjour le monde" }) } | ConvertTo-Json
Invoke-RestMethod -Method Post `
  -Uri "$endpoint/language/:analyze-text?api-version=2023-04-01" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key; "Content-Type"="application/json" } `
  -Body (@{ kind="LanguageDetection"; analysisInput=@{ documents=@(@{id="1"; text="Hola"}) } } | ConvertTo-Json -Depth 5)
```

---

## 18. Mental Model

> **NLP = text → tokens → vectors → model → label/answer/text. Today's best models are transformers; pair them with grounding (RAG) for accuracy and safety filters for responsibility.**
