-- Runs automatically ONLY on first container start, when db_data is empty.
-- For an existing dev database, run this file's contents manually (psql or pgAdmin).

CREATE EXTENSION IF NOT EXISTS vector;

-- vector(768) matches Ollama's nomic-embed-text model (see Ollama:EmbeddingModel
-- in appsettings.json). If you swap embedding models, this width must match
-- the new model's output dimension exactly or inserts will fail.
CREATE TABLE IF NOT EXISTS document_embeddings (
    id TEXT PRIMARY KEY,
    text TEXT NOT NULL,
    embedding vector(768),
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
