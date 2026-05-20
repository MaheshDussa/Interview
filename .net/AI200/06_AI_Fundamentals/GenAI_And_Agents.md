# Introduction to Generative AI & Agents

> **One-liner**: **GenAI = models that produce new content** (text, image, audio, code). **Agents = GenAI + tools + memory + goals** — they don't just answer, they **act**.

---

## 1. What is Generative AI?

GenAI predicts the **next token / pixel / sample** based on a learned probability distribution. Trained on massive datasets, it can:

- Write text (chat, summaries, code).
- Create images / video / audio.
- Translate, transform, classify.
- Reason step-by-step (with prompting).

**Analogy**: A very well-read intern with no internet access — knows lots, but only what it was trained on, until you give it tools.

---

## 2. Foundation Models

A **foundation model** is a large model pretrained on broad data, then adapted to many tasks.

| Modality | Examples |
|---|---|
| Text (LLMs) | GPT-4o, GPT-4.1, o-series, Llama, Mistral, Phi |
| Image | DALL·E 3, Stable Diffusion, FLUX |
| Audio / Speech | Whisper, gpt-4o-audio |
| Multimodal | GPT-4o (text+image+audio), Gemini |
| Embedding | text-embedding-3-large/small |
| Code | GPT-4.1, Codex, Code Llama |

> **Foundation model + fine-tune / RAG / prompts = your app**.

---

## 3. Tokens & Context Windows

- **Token** ≈ 4 chars / ~0.75 word in English.
- **Context window** = max tokens (input + output) in one call.
  - GPT-4o: 128k. GPT-4.1: 1M. Phi-4: smaller.
- You pay **per 1k tokens** (input + output priced separately).

```
"Hello, world!" ≈ 4 tokens
```

---

## 4. Prompt Anatomy

```
[System]   You are a helpful financial assistant. Be concise.
[User]     Summarize this Q3 report.
[Context]  <document text>
[Assistant] (model output)
```

| Part | Purpose |
|---|---|
| **System message** | Sets persona, rules, output format |
| **User message** | The actual ask |
| **Context** | Grounding documents/data |
| **Few-shot examples** | "Here are examples — do similar" |

---

## 5. Prompt Engineering Techniques

| Technique | What |
|---|---|
| **Zero-shot** | Just ask |
| **Few-shot** | Provide examples |
| **Chain-of-Thought (CoT)** | "Think step by step" |
| **ReAct** | Reason + Act (use tools) |
| **Self-consistency** | Sample multiple answers, vote |
| **Structured output** | Force JSON via schema / `response_format` |
| **Role / persona** | "You are a senior cloud architect" |
| **Delimiters** | Wrap content in `<doc>...</doc>` |

> **Memory hint**: **Clear, Specific, Show, Constrain** — CSSC.

---

## 6. Grounding & RAG

LLMs **hallucinate** when they don't know. Fix = **ground** them in real data.

### Retrieval-Augmented Generation (RAG)
```
User question → embed → vector DB search → top-k chunks
            → inject into prompt → LLM answers with citations
```

**Components:**
1. **Chunk** docs (~500 tokens with overlap).
2. **Embed** each chunk with an embedding model.
3. **Store** vectors (Azure AI Search, Cosmos DB, pgvector).
4. **Retrieve** top-k by cosine similarity.
5. **Generate** answer using only retrieved context.

> RAG = "open-book exam" for the LLM.

---

## 7. Fine-tuning vs RAG vs Prompting

| Need | Approach |
|---|---|
| Change tone / style | **Prompting** |
| Inject up-to-date company knowledge | **RAG** |
| Teach a new task / format consistently | **Fine-tune** |
| Combine all | All three together |

| | Prompt | RAG | Fine-tune |
|---|---|---|---|
| Cost | $ | $$ | $$$ |
| Latency | Fast | + retrieval | Fast |
| Freshness | Static | **Live** | Static |
| Best for | Behavior | Knowledge | Skill |

---

## 8. Embeddings & Vector Search

- **Embedding** = numeric vector (e.g. 1536-dim) capturing semantic meaning.
- **Similar text → similar vectors** (cosine similarity high).
- Vector stores: **Azure AI Search (vector + hybrid)**, **Cosmos DB** (NoSQL/Mongo vCore/PostgreSQL with pgvector), **Redis Enterprise**.

```python
# pseudo
v_query = embed("What is RAG?")
hits = index.search(vector=v_query, k=5)
```

**Hybrid search** = keyword (BM25) + vector + semantic reranker → best quality.

---

## 9. Azure OpenAI Service

- Microsoft-hosted **OpenAI models** with enterprise SLA, content filters, VNet, RBAC.
- Deploy a model into a **deployment** (gives you an endpoint + capacity).
- Capacity: **PTU** (Provisioned Throughput Units) for guaranteed throughput, or **Standard** (pay-per-token).

```csharp
// dotnet add package Azure.AI.OpenAI --prerelease
var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
var chat = client.GetChatClient("gpt-4o");
var resp = await chat.CompleteChatAsync(
    new SystemChatMessage("You are concise."),
    new UserChatMessage("What is RAG in 1 sentence?"));
Console.WriteLine(resp.Value.Content[0].Text);
```

---

## 10. Azure AI Foundry (umbrella)

- Portal + SDK to build AI apps end-to-end.
- **Model catalog** (Azure OpenAI + Llama + Mistral + Phi + serverless options).
- **Prompt flow** for orchestration.
- **Evaluations** (groundedness, relevance, fluency, safety).
- **Content Safety** built in.
- **Agents** (Foundry Agent Service).

---

## 11. What is an AI Agent?

**Agent = LLM + tools + memory + autonomy to act toward a goal.**

```
Goal → Plan → Act (call tool) → Observe → Re-plan → ... → Final answer
```

**Components**:
1. **LLM brain** (planner / reasoner).
2. **Tools** (functions, APIs, code interpreter, web search).
3. **Memory** (short-term context, long-term vector store).
4. **Orchestrator** (loop: think → act → observe).
5. **Policies / guardrails**.

**Analogy**: An intern with a phone, a browser, and your company directory — can look things up and take actions, not just talk.

---

## 12. Agent Patterns

| Pattern | What |
|---|---|
| **ReAct** | Reason + Act loop (most common) |
| **Plan-and-Execute** | Make a plan first, then execute each step |
| **Reflection** | Critique own output, revise |
| **Tool calling** | Model emits structured call to a function |
| **Multi-agent** | Specialized agents collaborate (planner, coder, reviewer) |
| **Human-in-the-loop** | Pause for human approval on key steps |

---

## 13. Function Calling (Tool Use)

LLM doesn't run code itself — it **emits JSON saying "call this function"**.

```csharp
var tools = new List<ChatTool>
{
    ChatTool.CreateFunctionTool("get_weather",
        "Get current weather for a city",
        BinaryData.FromString("""
        { "type":"object",
          "properties": { "city": {"type":"string"} },
          "required": ["city"] }
        """))
};

var resp = await chat.CompleteChatAsync(messages,
    new ChatCompletionOptions { Tools = { tools[0] } });

if (resp.Value.ToolCalls.Count > 0) {
    // execute, then send tool result back to model
}
```

---

## 14. Azure AI Agent Service (Foundry)

Managed agent runtime:
- Define agent (instructions, tools, knowledge sources).
- Tools: **Code Interpreter**, **File Search**, **Bing Grounding**, **Azure AI Search**, custom **Function Tools**, **OpenAPI** tools, **Logic Apps**.
- Threads & messages persisted automatically.
- Identity via Managed Identity.
- Observability via App Insights.

---

## 15. Multi-Agent Frameworks

| Framework | What |
|---|---|
| **Semantic Kernel** | Microsoft SDK (.NET/Python) for plugins, planners, agents |
| **AutoGen** | Multi-agent conversation framework (MS Research) |
| **LangChain / LangGraph** | Open-source orchestration |
| **Foundry Agent Service** | Managed agents on Azure |

---

## 16. Evaluation of GenAI

| Metric | What |
|---|---|
| **Groundedness** | Does answer stay in provided context? |
| **Relevance** | Does it answer the question? |
| **Coherence / Fluency** | Reads well |
| **Similarity** | Closeness to gold answer |
| **Safety** | Toxicity, hate, sexual, violence, self-harm |
| **Task-specific** | BLEU/ROUGE (summaries), pass@k (code) |

> Use **LLM-as-judge** (an LLM grades another LLM) for scaled qualitative evaluation.

---

## 17. Content Safety & Guardrails

- **Azure AI Content Safety**: image + text moderation, **prompt shields**, **groundedness detection**, **protected material detection**.
- Filter both **input** (prompt injection) and **output** (harmful content).
- Categories: hate, sexual, violence, self-harm.

---

## 18. Responsible GenAI Principles

| Principle | Practice |
|---|---|
| Disclose AI use | Tell users they're talking to AI |
| Cite sources | Show what RAG retrieved |
| Allow human override | Always provide opt-out / human path |
| Limit autonomy | Approvals on impactful actions (send email, charge card) |
| Log everything | Audit prompts, outputs, tool calls |
| Test for harm | Red-teaming, adversarial prompts |

---

## 19. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Hallucination | RAG + groundedness checks + cite sources |
| Prompt injection from documents | Use **Prompt Shields**, separate user vs system |
| Runaway agent loops | Set **max steps**, timeouts, cost budget |
| Token blow-up | Summarize history, trim context |
| Secrets in tools | Tools fetch creds via MI, never via prompt |
| Non-determinism in tests | Pin `temperature=0` for eval |
| Vendor lock-in | Use SDK abstractions (SK, OpenAI-compatible APIs) |

---

## 20. Interview / Exam Q&A

**Q1. LLM vs traditional ML?**
LLMs are pretrained on massive data and adapted via prompting/fine-tuning; traditional ML trains per task on smaller data.

**Q2. What is RAG and why use it?**
Retrieval-Augmented Generation injects fresh / private data into the prompt to ground the LLM and reduce hallucination.

**Q3. Prompt vs fine-tune?**
Prompt = teach via instruction at runtime (fast, cheap). Fine-tune = update weights (better for fixed format/skill).

**Q4. Token & context window?**
Token = sub-word unit. Context window = max tokens per call (input + output).

**Q5. What is an embedding?**
Numeric vector representing semantic meaning; similar items have similar vectors.

**Q6. Difference between an LLM call and an agent?**
LLM = stateless predictor. Agent = LLM + tools + memory + loop that pursues a goal autonomously.

**Q7. What is function calling?**
LLM emits structured JSON requesting a function be called; host runs it and feeds result back.

**Q8. Why hybrid search?**
Combines keyword (recall on exact terms) + vector (semantic) + semantic reranker for best retrieval quality.

**Q9. PTU vs Standard in Azure OpenAI?**
PTU reserves capacity (predictable latency). Standard is pay-as-you-go, may throttle.

**Q10. Prompt injection — what & defense?**
Malicious instructions inside user/data content try to override system prompt. Defenses: Prompt Shields, allowlists, sanitize untrusted content, separate trust zones.

**Q11. ReAct pattern?**
Loop of "Reason → Act (call tool) → Observe" until done.

**Q12. Difference between AutoGen, Semantic Kernel, and Foundry Agent Service?**
AutoGen = research framework for multi-agent. SK = production SDK with plugins/planners. Foundry = managed agent runtime on Azure.

---

## 21. Mental Model

> **GenAI = predict the next token. Make it useful with grounding (RAG), control (prompts/tools), evaluation, and safety. An agent is an LLM that can act — bound it with limits, logs, and human oversight.**
