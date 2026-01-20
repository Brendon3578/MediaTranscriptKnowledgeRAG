-- habilitar pgvector
CREATE EXTENSION IF NOT EXISTS vector;

-- =========================
-- tabela media
-- =========================
CREATE TABLE media (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_name VARCHAR(500) NOT NULL,
    file_path VARCHAR(1000) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP
);


-- =========================
-- tabela transcriptions
-- =========================
CREATE TABLE transcriptions (
    id UUID PRIMARY KEY,
    media_id UUID NOT NULL,
    text TEXT NOT NULL,
    language TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_transcription_media
        FOREIGN KEY (media_id)
        REFERENCES media(id)
        ON DELETE CASCADE
);

-- =========================
-- tabela transcription_segments
-- =========================
CREATE TABLE transcription_segments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transcription_id UUID NOT NULL,
    media_id UUID NOT NULL,
    segment_index INT NOT NULL,
    text TEXT NOT NULL,
    start_seconds REAL NOT NULL,
    end_seconds REAL NOT NULL,
    confidence REAL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_segment_transcription
        FOREIGN KEY (transcription_id)
        REFERENCES transcriptions(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_segment_media
        FOREIGN KEY (media_id)
        REFERENCES media(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_segments_media_id ON transcription_segments(media_id);
CREATE INDEX idx_segments_transcription_id ON transcription_segments(transcription_id);
CREATE INDEX idx_segments_media_index ON transcription_segments(media_id, segment_index);

-- =========================
-- tabela embeddings
-- =========================
CREATE TABLE embeddings (
    id UUID PRIMARY KEY,
    media_id UUID NOT NULL,
    transcription_id UUID NOT NULL,
    chunk_text TEXT NOT NULL,
    embedding VECTOR(768),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_embedding_media
        FOREIGN KEY (media_id)
        REFERENCES media(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_embedding_transcription
        FOREIGN KEY (transcription_id)
        REFERENCES transcriptions(id)
        ON DELETE CASCADE
);

-- =========================
-- �ndice vetorial (RAG)
-- =========================
CREATE INDEX embeddings_embedding_idx
ON embeddings
USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
