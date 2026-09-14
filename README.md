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

The AI Coach requires an environment configuration file.


## Tests

Backend:

```bash
dotnet test
```

Frontend:

```bash
npm --prefix web test
```
