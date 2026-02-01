# 📄 PRD — Sistema de Transcrição, Indexação Semântica e Busca (RAG)

## 1. Visão Geral

O sistema tem como objetivo permitir o upload de arquivos de mídia (áudio/vídeo), realizar a transcrição automática do conteúdo, gerar embeddings vetoriais por segmento e disponibilizar uma API de consulta semântica baseada em similaridade vetorial (RAG).

A arquitetura é orientada a serviços independentes, comunicando-se de forma assíncrona via eventos (RabbitMQ), com persistência em PostgreSQL + pgvector e uso de IA local (Whisper + Ollama).

## 2. Objetivos do Produto

### Objetivo Principal

Permitir que usuários façam buscas semânticas em conteúdos de mídia transcritos, retornando trechos relevantes com contexto temporal.

### Objetivos Secundários

- Desacoplar upload, transcrição, embeddings e consulta
- Suportar processamento assíncrono e escalável
- Manter todo o processamento on-premise / local
- Preparar base sólida para futuras extensões (resumo, insights, RAG avançado)

## 3. Escopo Atual (O que o sistema FAZ)

### ✅ Em escopo

- Upload de arquivos de mídia
- Persistência dos metadados da mídia
- Publicação de eventos de upload
- Transcrição automática usando Whisper (segmentada por tempo)
- Seleção dinâmica de modelo Whisper (Medium, Large, etc.)
- Persistência da transcrição, métricas e dos segmentos
- Geração de embeddings por segmento
- Persistência vetorial usando pgvector
- Busca semântica por similaridade
- Geração de respostas via LLM com base nos trechos recuperados

### ❌ Fora de escopo (por enquanto)

- Resumo automático
- Análise de sentimento
- Extração de insights
- Interface gráfica
- Autenticação/autorização
- Versionamento de embeddings
- Multi-tenant

## 4. Arquitetura Geral

### Estilo

- Arquitetura orientada a eventos
- Processamento assíncrono
- Separação clara de responsabilidades
- IA local (sem dependência de APIs externas)

### Componentes Principais

| Serviço | Responsabilidade |
| :--- | :--- |
| **Upload.Api** | Receber arquivos e publicar eventos |
| **Transcription.Worker** | Transcrever mídia em texto segmentado |
| **Embedding.Worker** | Gerar embeddings por segmento |
| **Query.Api** | Executar buscas semânticas e RAG |
| **Shared** | Contratos e eventos compartilhados |

## 5. Fluxo Principal do Sistema

### 5.1 Upload

1. Usuário envia arquivo para **Upload.Api** (opcionalmente escolhendo o modelo Whisper)
2. Arquivo é salvo localmente
3. Metadados são persistidos no banco
4. Evento `MediaUploadedEvent` é publicado no RabbitMQ

### 5.2 Transcrição

1. **Transcription.Worker** consome `MediaUploadedEvent`
2. Áudio é extraído/convertido
3. Whisper realiza a transcrição (usando modelo solicitado ou default)
4. Texto é segmentado com timestamps
5. Transcrição, métricas (tempo, modelo) e segmentos são salvos no banco
6. Evento `MediaTranscribedEvent` é publicado

### 5.3 Geração de Embeddings

1. **Embedding.Worker** consome `MediaTranscribedEvent`
2. Segmentos ainda não processados são carregados
3. Embeddings são gerados via Ollama
4. Vetores são persistidos no PostgreSQL (pgvector)
5. Processo é idempotente por (segmento + modelo)

### 5.4 Consulta Semântica (RAG)

1. Usuário envia pergunta para **Query.Api**
2. Pergunta é convertida em embedding
3. Consulta vetorial é executada ( `<=>` )
4. Segmentos mais próximos são retornados
5. Contexto é montado com timestamps
6. LLM gera resposta baseada no contexto

## 6. Modelo de Dados (Alto nível)

### media

- `id` (UUID)
- `file_name` (VARCHAR)
- `file_path` (VARCHAR)
- `content_type` (VARCHAR)
- `status` (INTEGER)
- `file_size_bytes` (BIGINT)
- `created_at` (TIMESTAMP)
- `updated_at` (TIMESTAMP)

### transcriptions

- `id` (UUID)
- `media_id` (UUID)
- `text` (TEXT)
- `language` (TEXT)
- `model_name` (TEXT)
- `processing_time_seconds` (REAL)
- `created_at` (TIMESTAMP)

### transcription_segments

- `id` (UUID)
- `transcription_id` (UUID)
- `media_id` (UUID)
- `segment_index` (INT)
- `text` (TEXT)
- `start_seconds` (REAL)
- `end_seconds` (REAL)
- `confidence` (REAL)
- `created_at` (TIMESTAMP)

### embeddings

- `id` (UUID)
- `media_id` (UUID)
- `transcription_id` (UUID)
- `transcription_segment_id` (UUID)
- `model_name` (TEXT)
- `chunk_text` (TEXT)
- `embedding` (VECTOR)
- `created_at` (TIMESTAMP)

## 7. Tecnologias Utilizadas

### Backend

- .NET 10
- ASP.NET Web API
- Background Workers

### IA

- Whisper.NET (transcrição)
- Ollama (embeddings e chat)
- Microsoft.Extensions.AI

### Infraestrutura

- RabbitMQ
- PostgreSQL
- pgvector
- Docker

### Acesso a dados

- Entity Framework Core (escrita)
- SQL + Npgsql (consulta vetorial)

## 8. Requisitos Não Funcionais

- Processamento assíncrono
- Idempotência na geração de embeddings
- Tolerância a falhas em workers
- Observabilidade via logs
- Baixo acoplamento entre serviços
- Código legível e manutenível

## 9. Premissas e Restrições

### Premissas

- Modelos de IA rodam localmente
- Banco único compartilhado
- RabbitMQ é o backbone de eventos

### Restrições

- Sem UI
- Sem orquestração complexa
- Sem versionamento de modelos

## 10. Estado Atual do Produto

✅ **Produto funcional**
