---
alwaysApply: false
globs: **/*.sql,**/docker-compose*.yml
---
# Database Implementation Standards

- **Database Engine**: The application uses **PostgreSQL** (with pgvector) running via **Docker**.
- **Schema Management**:
  - All database schema updates (tables, indexes, extensions, initial data) MUST be made in `docker/setup.sql`.
  - Do not create separate migration files unless the strategy changes.
  - The `setup.sql` file is the source of truth for the database state.

# Workflow

- When adding a new table or modifying an existing one, edit `docker/setup.sql` directly.
- Ensure `pgvector` extension is enabled if working with embeddings.
