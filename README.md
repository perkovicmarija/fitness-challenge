# Fitness Challenge

```bash
docker compose up --build
```

Then open **http://localhost:8080**. Nothing else to install.

Without Docker, `./run.sh` needs .NET 10 and Node 22 and serves the app on 4200.

The **chat** in the AI coach needs an Azure OpenAI key — copy `.env.example` to `.env`. Without
one the screen still works: its rank, gap and recommendation are calculated by the app, and only
the chat box reports itself switched off.

```bash
dotnet test
npm --prefix web test
```

