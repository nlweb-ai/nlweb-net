# Real Vector Search Configuration

This application now supports **real semantic embeddings** using GitHub Models for proper vector search.

## Option 1: Use GitHub Models (Recommended)

To enable real semantic vector search with GitHub Models:

1. Get a GitHub personal access token with model access from <https://github.com/settings/tokens>
   - Ensure you have access to GitHub Models in your account

2. Set the environment variable:

   ```bash
   # Windows PowerShell
   $env:GITHUB_TOKEN="your-github-token-here"
   
   # Windows Command Prompt
   set GITHUB_TOKEN=your-github-token-here
   
   # Linux/Mac
   export GITHUB_TOKEN="your-github-token-here"
   ```

3. Restart the application

When a GitHub token is available, the application will:

- Use `text-embedding-3-small` model for generating semantic embeddings
- Provide high-quality vector search results  
- Enable proper semantic similarity matching
- Work seamlessly with the existing `/samples/Demo` app pattern

## Option 2: Use OpenAI Embeddings (Alternative)

If you prefer OpenAI over GitHub Models:

1. Get an OpenAI API key from <https://platform.openai.com/>

2. Set the environment variable:

   ```bash
   # Windows PowerShell
   $env:OPENAI_API_KEY="your-openai-api-key-here"
   
   # Windows Command Prompt
   set OPENAI_API_KEY=your-openai-api-key-here
   
   # Linux/Mac
   export OPENAI_API_KEY="your-openai-api-key-here"
   ```

3. Restart the application

When an OpenAI API key is available, the application will:

- Use `text-embedding-ada-002` model for generating semantic embeddings
- Provide high-quality vector search results
- Enable proper semantic similarity matching

## Option 3: Simple Embeddings (Demo Only)

If no GitHub token or OpenAI API key is provided, the application falls back to:

- Simple hash-based embeddings (not semantically meaningful)
- Random similarity scores
- Intended only for basic functionality testing

## Testing Vector Search

Once configured with OpenAI embeddings:

1. Clear existing data: `DELETE https://localhost:7220/vector/clear`
2. Ingest demo feeds: `POST https://localhost:7220/rss/ingest-demo`
3. Search with semantic queries:
   - `GET https://localhost:7220/api/search?query=copilot&limit=5`
   - `GET https://localhost:7220/api/search?query=.NET%2010&limit=5`
   - `GET https://localhost:7220/api/search?query=artificial%20intelligence&limit=5`

The search results should now be semantically relevant to your query terms.

## Expected Behavior

With GitHub Models or OpenAI embeddings:

- ✅ Search for "copilot" returns GitHub Copilot and AI assistant related posts
- ✅ Search for ".NET 10" returns posts about .NET 10 previews and features
- ✅ Search for "AI" returns artificial intelligence and machine learning posts
- ✅ Similar concepts return similar results (e.g., "AI" and "machine learning")

Without real embeddings:

- ❌ Random/irrelevant results
- ❌ No semantic understanding
- ❌ Same results regardless of query
