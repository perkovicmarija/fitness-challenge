# Fitness Challenge

## Run

### Docker

```bash
docker compose up --build
```

Application: http://localhost:8080

### Local

Requirements:

* .NET 10
* Node 22

```bash
./run.sh
```

Application: http://localhost:4200

## AI Coach

The chat needs an Azure OpenAI key — copy `.env.example` to `.env` and fill it in.

Without one the screen still works: rank, gap and recommendation are calculated by the app.
Only the chat box reports itself switched off.


## Tests

Backend:

```bash
dotnet test
```

Frontend:

```bash
npm --prefix web test
```
