# One image serves both halves. Node builds the Angular app, the .NET SDK publishes the API, and
# the runtime image serves the API with the built site in wwwroot. Same origin, so there is no
# CORS layer to configure and no second container to keep in step.

FROM node:22-alpine AS web
WORKDIR /web
# npm 10 fails to resolve this dependency tree (an optional peer of jsdom). See the README.
RUN npm install --global npm@12
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /build
COPY Directory.Build.props ./
COPY src/ src/
RUN dotnet publish src/FitnessChallenge.Api -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /publish ./
COPY --from=web /web/dist/fitness-challenge-web/browser ./wwwroot

# The database is a single file, kept on a volume so rebuilding the image does not reset the
# challenge. See docs/contract.md for why the schema is created on startup.
RUN mkdir -p /data
ENV ConnectionStrings__Database="Data Source=/data/fitness-challenge.db"
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitnessChallenge.Api.dll"]
