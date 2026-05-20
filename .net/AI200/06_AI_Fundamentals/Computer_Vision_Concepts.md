# Introduction to Computer Vision

> **One-liner**: **Computer Vision = teach computers to see** — turn pixels into structured information (objects, text, faces, scenes, motion).

---

## 1. What Computer Vision Can Do

| Task | Example output |
|---|---|
| **Image classification** | "Dog: 0.97" |
| **Object detection** | Bounding boxes + labels |
| **Image segmentation** | Pixel-level mask per object |
| **Instance segmentation** | Per-instance masks |
| **OCR** | Read printed/handwritten text |
| **Face detection / recognition** | Find / identify people |
| **Image captioning** | "A dog catches a frisbee" |
| **Visual question answering (VQA)** | Answer questions about image |
| **Spatial analysis** | Count people, dwell time |
| **Video action recognition** | What is happening |
| **Image generation** | DALL·E, Stable Diffusion |

---

## 2. Image Basics

| Term | Meaning |
|---|---|
| **Pixel** | Smallest image unit (RGB or grayscale value) |
| **Resolution** | Width × height in pixels |
| **Channel** | Color dimension (R, G, B, sometimes A or depth) |
| **Tensor** | NN input shape `[batch, H, W, C]` |
| **Bounding box** | `(x, y, w, h)` rectangle |
| **Mask** | 2D label per pixel |
| **IoU** | Intersection over Union (overlap quality) |
| **mAP** | Mean Average Precision (detection metric) |

---

## 3. Classical → Deep Learning Evolution

| Era | Approach |
|---|---|
| Pre-2012 | Hand-crafted features (SIFT, HOG) + SVM |
| 2012 | **AlexNet** wins ImageNet → deep learning takes over |
| 2015 | **ResNet** — very deep networks via skip connections |
| 2017 | **Mask R-CNN** — detection + segmentation |
| 2020+ | **Vision Transformers (ViT)** — attention beats CNN at scale |
| 2023+ | **Multimodal** models (GPT-4o, Gemini) — text + image together |

---

## 4. Convolutional Neural Networks (CNN) — the workhorse

```
Image → Conv layer (filters) → Pooling → Conv → ... → Flatten → Dense → Softmax
```

- **Filters** learn edges → textures → parts → objects.
- **Pooling** reduces spatial size (downsample).
- Trained with cross-entropy loss + SGD/Adam.

---

## 5. Object Detection Architectures

| Family | Examples | Style |
|---|---|---|
| **Two-stage** | R-CNN, Fast/Faster R-CNN, Mask R-CNN | Propose then classify — accurate, slower |
| **One-stage** | YOLO (v1-v10), SSD, RetinaNet | Predict directly — fast, real-time |
| **Transformer-based** | DETR, Grounding DINO | End-to-end, query-based |

---

## 6. Azure AI Vision Service

| Feature | What |
|---|---|
| **Image Analysis** | Tags, captions, objects, people, smart crops, dense captions |
| **OCR (Read API)** | Printed + handwritten in 100+ languages |
| **Background removal** | Subject mask |
| **Face API** | Detect, verify, identify, find similar (Limited Access) |
| **Spatial Analysis** | Count people, dwell, social distancing (containerized) |
| **Video Indexer** | Faces, OCR, transcript, topics in video |
| **Custom Vision** | Train your own classifier / detector |

---

## 7. Image Analysis 4.0 — Visual Features

Calling `analyze` you choose features:

| Feature | Output |
|---|---|
| `caption` | One-sentence description |
| `denseCaptions` | Per-region captions |
| `objects` | Bounding boxes + labels |
| `tags` | Whole-image labels |
| `people` | People bounding boxes |
| `read` | OCR text + lines + words + polygons |
| `smartCrops` | Auto-crop suggestions for aspect ratios |

---

## 8. .NET SDK Example — `Azure.AI.Vision.ImageAnalysis`

```csharp
// dotnet add package Azure.AI.Vision.ImageAnalysis

var client = new ImageAnalysisClient(
    new Uri("https://<resource>.cognitiveservices.azure.com/"),
    new DefaultAzureCredential());

var result = await client.AnalyzeAsync(
    new Uri("https://example.com/image.jpg"),
    VisualFeatures.Caption | VisualFeatures.Tags
        | VisualFeatures.Objects | VisualFeatures.Read,
    new ImageAnalysisOptions { GenderNeutralCaption = true });

Console.WriteLine(result.Value.Caption.Text);
foreach (var tag in result.Value.Tags.Values) Console.WriteLine(tag.Name);
foreach (var line in result.Value.Read.Blocks[0].Lines) Console.WriteLine(line.Text);
```

---

## 9. OCR (Read)

- Detects **printed + handwritten** text.
- Returns **lines, words, polygons, confidence**.
- Supports 160+ printed languages and many handwritten.
- For structured docs (invoices/receipts), use **Azure AI Document Intelligence** instead.

---

## 10. Custom Vision

Build domain-specific image models without writing ML code.

| Project type | Use |
|---|---|
| **Classification (multi-class)** | One label per image |
| **Classification (multi-label)** | Many labels per image |
| **Object detection** | Bounding boxes |

Flow: upload + label → train → evaluate → publish → call REST/SDK.

Two domains:
- **Standard** (cloud).
- **Compact** (export to ONNX/TF/CoreML for edge devices).

Metrics shown: **Precision, Recall, AP (per class), mAP**.

---

## 11. Face Service (Limited Access)

- **Detect** facial landmarks, attributes (glasses, occlusion, pose).
- **Verify** (1:1) — same person?
- **Identify** (1:N) — match against PersonGroup.
- **Find similar** faces.

> Highly regulated: **face identification/verification require approval** (Responsible AI). Emotion / gender / age inferences were retired.

---

## 12. Multimodal Vision via Azure OpenAI

GPT-4o / GPT-4.1 accept **image inputs**:

```csharp
var resp = await chat.CompleteChatAsync(new ChatMessage[] {
    new UserChatMessage(
        ChatMessageContentPart.CreateTextPart("Describe what's wrong in this picture."),
        ChatMessageContentPart.CreateImagePart(new Uri("https://.../img.png")))
});
```

Use for: open-ended visual QA, table/chart understanding, UI element extraction, accessibility descriptions.

---

## 13. Evaluation Metrics

| Task | Metric |
|---|---|
| Classification | Accuracy, Precision/Recall/F1, Top-5 acc |
| Detection | **IoU**, Precision/Recall, **mAP@0.5**, **mAP@0.5:0.95** |
| Segmentation | IoU per class, Dice coefficient |
| OCR | CER / WER, edit distance |
| Face verification | TAR @ FAR (e.g., TAR ≥ 99% at FAR = 0.1%) |

### IoU
```
IoU = (Area of overlap) / (Area of union)
```
Detection counts as TP if IoU ≥ threshold (usually 0.5).

---

## 14. Data & Augmentation

Models hunger for data. Augment by:
- Horizontal flip, rotate, crop, zoom.
- Color jitter, brightness, contrast.
- Cutout / mixup / mosaic.
- Synthetic data via generative models.

Annotate with tools: **Azure ML data labeling**, CVAT, Labelbox, Roboflow.

---

## 15. Edge & On-Device Vision

| Option | Notes |
|---|---|
| **ONNX Runtime** | Cross-platform inference |
| **TensorFlow Lite / CoreML** | Mobile (Android / iOS) |
| **Custom Vision compact export** | Export to ONNX/TF/CoreML |
| **NVIDIA Triton** / **OpenVINO** | High-perf server-side |
| **Spatial Analysis container** | Run on-prem with GPU |

---

## 16. Responsible AI & Privacy

| Concern | Mitigation |
|---|---|
| Face recognition misuse | Limited Access program, consent, retention limits |
| Surveillance bias | Diverse training data, fairness audits |
| Children's data | Extra protections (COPPA, etc.) |
| Generated/synthetic media | Disclose, watermark (C2PA / Content Credentials) |
| Body-imagery moderation | Use **Azure AI Content Safety — Image** |

---

## 17. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Tiny custom dataset (~5 images/class) | Need 50+; use augmentation; transfer learning |
| Class imbalance | Oversample, class weights |
| Wrong evaluation threshold | Sweep IoU/confidence; use PR curves |
| Mixing OCR with form extraction | Use **Document Intelligence** for forms |
| 90° rotated images | Auto-orient or include rotations in training |
| Mobile model too large | Use compact / quantized models |
| Color space mismatch | Stay in RGB; document channel order |

---

## 18. Architecture Patterns

### Retail — shelf compliance
```
Camera → Blob → Function (Event Grid trigger)
       → Custom Vision (object detection)
       → Cosmos DB (counts) → Power BI
```

### Document workflow
```
Upload → Document Intelligence (forms)
       + Image Analysis (page caption)
       → Search index → Q&A app
```

### Multimodal RAG
```
PDFs / images → Caption + OCR + embed
              → Azure AI Search (vector + hybrid)
              → GPT-4o answers with image evidence
```

---

## 19. Interview / Exam Q&A

**Q1. Difference between classification, detection, segmentation?**
Classification = label whole image. Detection = boxes around objects. Segmentation = pixel-level mask.

**Q2. What is IoU?**
Intersection over Union — overlap ratio between predicted and ground truth boxes/masks.

**Q3. mAP?**
Mean Average Precision over all classes (often at IoU=0.5 or averaged 0.5-0.95).

**Q4. Why use transfer learning?**
Pretrained backbone on ImageNet/large data gives strong features → little data needed for your task.

**Q5. Custom Vision vs Image Analysis?**
Image Analysis = prebuilt general features. Custom Vision = your domain model with your labels.

**Q6. OCR vs Document Intelligence?**
OCR returns raw text + locations. Document Intelligence understands structure (key-value, tables, forms, prebuilt invoice/receipt models).

**Q7. Why was emotion/gender/age detection retired?**
Responsible AI concerns about reliability and harm; Face service narrowed scope.

**Q8. CNN vs ViT?**
CNNs use convolutional filters with local receptive fields. ViTs apply transformer self-attention to image patches; scale better with data.

**Q9. Edge deployment?**
Export to ONNX/TFLite/CoreML, run on device with ONNX Runtime / TFLite / CoreML for offline + low-latency.

**Q10. How to count people while preserving privacy?**
Use **Spatial Analysis** for counts/dwell (no identity) on-prem container, blur faces in any stored frames.

---

## 20. CLI / REST Cheat Sheet

```powershell
# Analyze image (4.0)
$endpoint = "https://<res>.cognitiveservices.azure.com"
$body = @{ url = "https://example.com/cat.jpg" } | ConvertTo-Json
Invoke-RestMethod -Method Post `
  -Uri "$endpoint/computervision/imageanalysis:analyze?api-version=2024-02-01&features=caption,read,objects,tags" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key; "Content-Type"="application/json" } `
  -Body $body
```

---

## 21. Mental Model

> **Computer Vision = pixels → features → predictions. Use Image Analysis for general tasks, OCR/Document Intelligence for text, Custom Vision for your domain, GPT-4o for open-ended visual reasoning, and handle face data under strict Responsible AI rules.**
