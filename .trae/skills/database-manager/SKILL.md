---
name: database-manager
description: Manage database schema and entity synchronization. Invoke when the user asks to modify tables, update database schema, change entities, or modify setup.sql.
---

# Database Schema & Entity Manager

This skill guides the process of modifying the database schema and synchronizing the application code (Entities and DbContext).

## 1. Source of Truth: `docker/setup.sql`

* **Rule**: The `docker/setup.sql` file is the **SINGLE SOURCE OF TRUTH** for the database schema.
* **Action**: Always start by modifying `docker/setup.sql`.
* **Constraint**: Do not create separate migration files (e.g., Entity Framework migrations).
* **PGVector**: Ensure `CREATE EXTENSION IF NOT EXISTS vector;` is preserved if using embeddings.

## 2. Global Synchronization Rule

**CRITICAL**: Any change in `docker/setup.sql` MUST be reflected in **ALL** relevant files across the ENTIRE project:

1. **ALL Entity Classes**: Update every Entity class that maps to the modified table.
2. **ALL DbContext Files**: Update every `DbContext` (e.g., `UploadDbContext`, `EmbeddingDbContext`, `QueryDbContext`) that includes the modified table.

## 3. Entity Configuration (Domain Layer)

When mapping the SQL table to a C# Entity class:

* **Class Name**: Should match the concept (e.g., `MediaEntity`).
* **Properties**: Must match the SQL columns exactly.
* **Attributes**: You **MUST** use the `[Column("name")]` attribute for every property to specify the exact database column name.

### Example

```csharp
public class MyEntity
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("file_name")]
    public string FileName { get; set; }
}
```

## 4. DbContext Configuration (Infrastructure Layer)

When configuring the `DbContext` in `OnModelCreating`:

* **Property Access**: Use `e.Property(e => e.PropertyName)` for explicit configuration.
* **Optionality Rule**:
  * **Primary Key**: Configure normally (e.g., `e.HasKey(...)`).
  * **Context**: "All attributes are optionals less the primary key column".

### Example Pattern

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<MyEntity>(e =>
    {
        e.ToTable("my_table");
        
        // Primary Key - Required
        e.HasKey(e => e.Id);

        // Other properties - Use e.Property()
        e.Property(e => e.FileName)
            .HasMaxLength(500); // MaxLength is okay

        e.Property(e => e.Description); 
    });
}
```

## Workflow Checklist

1. [ ] **Modify `docker/setup.sql`**: Add/Change tables or columns.
2. [ ] **Scan Project**: Identify **ALL** Entity classes and DbContexts that reference the table.
3. [ ] **Update ALL Entities**: Add properties with `[Column("name")]`.
4. [ ] **Update ALL DbContexts**: Add `e.Property(...)` mappings in `OnModelCreating`.
