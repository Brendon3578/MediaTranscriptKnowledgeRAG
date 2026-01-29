# 🧠 PRD — Plataforma de Transcrição Segmentada + Embeddings (RAG-ready)

## 1️⃣ Visão Geral

Sistema baseado em **microserviços orientados a eventos** para:

* Upload de áudio/vídeo
* Transcrição **segmentada por tempo**
* Geração futura de **embeddings por segmento**
* Consulta semântica via LLM (RAG)

Arquitetura pensada para:

* reprocessamento
* auditoria
* evolução de modelos
* explicabilidade (timestamp)

---

## 2️⃣ Objetivos do Produto

✔ Transcrever mídia em **segmentos temporais**
✔ Permitir **busca semântica precisa**
✔ Associar respostas a **momentos do vídeo/áudio**
✔ Ser **agnóstico de IA** (local ou cloud)
✔ Escalar por eventos (RabbitMQ)

---

## 3️⃣ Fluxo Geral (Event-Driven)

```txt
Upload.Api
  → media.uploaded
      ↓
Transcription.Worker
  → transcrição segmentada
  → media.transcribed
      ↓
Embedding.Worker
  → embedding por segmento
      ↓
Query.Api (RAG)
```

---

## 4️⃣ Microserviços e Responsabilidades

### 📤 Upload.Api

* Recebe arquivo
* Salva localmente
* Registra `media`
* Publica `MediaUploadedEvent`

---

### 🎙 Transcription.Worker

* Consome `media.uploaded`
* Extrai áudio (FFmpeg)
* Transcreve via Whisper local
* **Gera segmentos com timestamps**
* Salva segmentos no banco
* Publica `MediaTranscribedEvent`

---

### 🧠 Embedding.Worker (futuro)

* Consome `media.transcribed`
* Gera embedding **por segmento**
* Salva no pgvector

---

### 🔍 Query.Api (futuro)

* Busca semântica por embedding
* Retorna:

  * texto
  * timestamp
  * referência de mídia

---

## 5️⃣ Modelo de Dados (PostgreSQL + pgvector)

### 📄 media

```sql
id UUID PK
file_name TEXT
file_path TEXT
content_type TEXT
duration_seconds INT
status TEXT
created_at TIMESTAMP
```

---

### 📝 transcriptions

```sql
id UUID PK
media_id UUID FK
language TEXT
model_used TEXT
created_at TIMESTAMP
```

---

### 🧩 transcription_segments ⭐ (NOVA BASE)

```sql
id UUID PK
transcription_id UUID FK
media_id UUID FK
segment_index INT
text TEXT
start_seconds REAL
end_seconds REAL
confidence REAL
created_at TIMESTAMP
```

 🔑 **Essa tabela é a base do RAG**

---

### 📐 embeddings (futuro)

```sql
id UUID PK
segment_id UUID FK
vector VECTOR(1536)
created_at TIMESTAMP
```

---

## 6️⃣ Decisões Importantes (Justificadas)

### Por que segmentar?

* Whisper já retorna segmentos
* Embedding por segmento = respostas melhores
* Evita chunking artificial depois

### Por que não salvar texto inteiro?

* Perde contexto temporal
* Embedding fica “genérico”
* Não permite referência ao tempo

---

## 7️⃣ Eventos (contratos)

### MediaUploadedEvent

* unchanged

### MediaTranscribedEvent

```json
{
  "mediaId": "guid",
  "transcriptionId": "guid",
  "segmentsCount": 42
}
```

---

## 8️⃣ Métricas importantes (futuro)

* tempo total de transcrição
* duração média por segmento
* tokens por segmento
* custo por mídia
