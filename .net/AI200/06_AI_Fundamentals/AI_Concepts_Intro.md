# Introduction to AI Concepts

> **One-liner**: **AI = software that mimics human cognitive abilities** — learning from data, making predictions, understanding language, seeing, and reasoning.

---

## 1. What is AI?

**Artificial Intelligence (AI)** is software that performs tasks that normally require human intelligence:

- **Perceive** (vision, speech)
- **Understand** (language, intent)
- **Learn** (from examples / experience)
- **Reason** (decide, plan)
- **Interact** (talk, act)

**Analogy**: A baby learning by watching parents — pattern recognition + correction loop.

---

## 2. AI vs ML vs DL vs GenAI

```
AI (broad concept)
└── Machine Learning (ML) — learn from data
    └── Deep Learning (DL) — neural networks with many layers
        └── Generative AI — produces new content (LLMs, image gen)
```

| Term | What | Example |
|---|---|---|
| **AI** | Any intelligent behavior in software | Rule-based chatbot |
| **ML** | System learns patterns from data | Spam filter |
| **DL** | ML using deep neural networks | Image classifier (ResNet) |
| **GenAI** | Creates new text/image/audio | ChatGPT, DALL·E |

---

## 3. Core AI Workloads (Azure AI categories)

| Workload | What it does | Azure service |
|---|---|---|
| **Machine Learning** | Train predictive models | Azure Machine Learning |
| **Computer Vision** | Analyze images/video | Azure AI Vision |
| **NLP** | Understand/process text | Azure AI Language |
| **Speech** | Speech↔text, translation | Azure AI Speech |
| **Document Intelligence** | Extract from forms/docs | Azure AI Document Intelligence |
| **Knowledge Mining** | Search across many sources | Azure AI Search |
| **Generative AI** | Create new content | Azure OpenAI Service |
| **Decision** | Anomaly detect, personalize | Anomaly Detector, Personalizer |

---

## 4. Types of Machine Learning

| Type | What | Example |
|---|---|---|
| **Supervised** | Labeled data (input → known output) | Email spam (spam/not spam) |
| **Unsupervised** | No labels, find structure | Customer clustering |
| **Semi-supervised** | Some labels | Mix |
| **Reinforcement** | Reward signal | Game-playing AI, robotics |
| **Self-supervised** | Generate labels from data itself | LLM pre-training |

### Supervised sub-types
| | Output | Example |
|---|---|---|
| **Regression** | Continuous number | House price |
| **Classification** | Discrete category | Disease yes/no |
| **Multi-class** | One of N | Animal species |
| **Multi-label** | Many of N | Image tags |

---

## 5. The ML Workflow (8 steps)

```
1. Define problem
2. Collect & label data
3. Clean & explore (EDA)
4. Feature engineering
5. Train model
6. Evaluate (metrics)
7. Deploy (endpoint)
8. Monitor & retrain
```

> Real-world rule: **80% of effort is data prep**, only 20% is modeling.

---

## 6. Common Evaluation Metrics

### Classification
| Metric | Formula | When |
|---|---|---|
| **Accuracy** | (TP+TN) / Total | Balanced classes |
| **Precision** | TP / (TP+FP) | Cost of false alarms high |
| **Recall** | TP / (TP+FN) | Cost of missed positives high |
| **F1** | 2·P·R / (P+R) | Balance both |
| **AUC-ROC** | Area under curve | Threshold-agnostic |

### Regression
- **MAE** — Mean Absolute Error
- **MSE / RMSE** — penalize large errors
- **R²** — variance explained

> **Memory hint**: Precision = "of my **alerts**, how many are real?" Recall = "of all **real cases**, how many did I catch?"

---

## 7. Key Vocabulary

| Term | Meaning |
|---|---|
| **Feature** | Input variable |
| **Label / Target** | What you predict |
| **Training set** | Data used to learn |
| **Validation set** | Used to tune hyperparameters |
| **Test set** | Final, held-out evaluation |
| **Overfitting** | Model memorizes training data, fails on new |
| **Underfitting** | Model too simple to capture pattern |
| **Bias** | Systematic error / unfairness |
| **Variance** | Sensitivity to training data |
| **Hyperparameter** | Knob you set (e.g., learning rate) |
| **Inference** | Using a trained model to predict |
| **Embedding** | Vector representation of data |
| **Token** | Smallest text unit a model processes |

---

## 8. Neural Network Basics

```
[input] → [layer 1] → [layer 2] → ... → [output]
            ↑ weights are learned via back-propagation
```

- **Neuron** = weighted sum + activation function (ReLU, sigmoid, softmax).
- **Backprop** adjusts weights to minimize loss.
- **Loss function** measures prediction error (MSE, cross-entropy).
- **Optimizer** updates weights (SGD, Adam).

### Architectures (memorize)
| Architecture | Use case |
|---|---|
| **CNN** (Convolutional) | Images |
| **RNN / LSTM** | Sequences (older) |
| **Transformer** | Modern NLP & vision; powers LLMs |
| **GAN** | Generate images (older) |
| **Diffusion** | Image generation (DALL·E 3, SD) |

---

## 9. Responsible AI — Microsoft 6 Principles

| Principle | Means |
|---|---|
| **Fairness** | No biased treatment of groups |
| **Reliability & Safety** | Predictable behavior, fails gracefully |
| **Privacy & Security** | Protect data, respect consent |
| **Inclusiveness** | Useful to all people |
| **Transparency** | Explain how decisions are made |
| **Accountability** | Humans responsible for the system |

**Memory hint**: **F**RPIT**A** → "FRPITA" (Fairness, Reliability, Privacy, Inclusiveness, Transparency, Accountability).

---

## 10. AI Risks & Mitigations

| Risk | Mitigation |
|---|---|
| **Bias in training data** | Diverse data, fairness metrics, Fairlearn |
| **Hallucination (LLMs)** | Grounding/RAG, citations, human review |
| **Data privacy** | Differential privacy, anonymization, on-device |
| **Prompt injection** | Input filtering, system prompts, Azure AI Content Safety |
| **Lack of explainability** | SHAP, LIME, InterpretML |
| **Model drift** | Continuous monitoring + retraining |
| **Misuse / abuse** | Content filters, usage policies, audit logs |

---

## 11. Azure AI Foundry / Studio (umbrella)

- **Azure AI Foundry** = single portal to build with Azure OpenAI, AI services, custom models.
- **Multi-service AI account** = one key for many services (Vision, Language, Speech).
- **Single-service accounts** for cost separation.

---

## 12. Pricing Models

| Style | Example |
|---|---|
| Pay-per-transaction | Vision API call |
| Pay-per-token | Azure OpenAI |
| Pay-per-compute-hour | Azure ML compute |
| Free F0 tier | Limited quota for dev |

---

## 13. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Using accuracy on imbalanced data | Use F1, AUC, confusion matrix |
| Data leakage (test info in training) | Strict train/val/test split, time-based for time series |
| Tiny training set | Augmentation, transfer learning |
| No baseline | Always compare to dummy / linear model |
| Ignoring monitoring | Watch for **drift** in features & predictions |
| Treating model as oracle | Always pair with human review for high-stakes |

---

## 14. Interview / Exam Q&A

**Q1. Difference between AI, ML, DL, GenAI?**
AI = broad mimicry of intelligence. ML = AI that learns from data. DL = ML using deep neural nets. GenAI = DL that creates new content.

**Q2. Supervised vs unsupervised?**
Supervised uses labeled data (X→y); unsupervised finds structure in unlabeled data (clusters, density).

**Q3. Why split data into train/validation/test?**
Train = learn, Validation = tune hyperparameters, Test = unbiased final evaluation.

**Q4. Overfitting symptoms & fixes?**
Train accuracy high, test low. Fix: more data, regularization, dropout, simpler model, early stopping.

**Q5. What's a confusion matrix?**
2×2 table of TP/FP/FN/TN used to derive precision/recall/F1.

**Q6. Microsoft Responsible AI principles?**
Fairness, Reliability & Safety, Privacy & Security, Inclusiveness, Transparency, Accountability.

**Q7. What is an embedding?**
A dense vector representation of input (word, sentence, image) so semantic similarity = vector distance.

**Q8. When to retrain a model?**
On data drift, concept drift, performance drop, new data availability.

**Q9. AutoML vs custom ML?**
AutoML auto-selects algorithms & hyperparameters. Custom = data scientist writes the pipeline.

**Q10. What is transfer learning?**
Start with a pretrained model and fine-tune for your task — saves data & compute.

---

## 15. Quick Glossary Cheat Sheet

| Term | One-line |
|---|---|
| Model | Trained function: input → output |
| Dataset | Collection of examples |
| Inference | Running the model in production |
| Endpoint | URL exposing the model |
| Loss | Error to minimize |
| Epoch | One full pass over training data |
| Batch | Subset of examples per gradient step |
| Tensor | N-D array (input/output to NN) |
| GPU/TPU | Accelerator hardware |
| RAG | Retrieval-Augmented Generation |

---

## 16. Mental Model

> **AI = data + algorithm + compute → model → predictions. Always evaluate with held-out data, watch for drift, and follow Responsible AI principles.**
