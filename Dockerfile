FROM node:22-alpine AS web
WORKDIR /web
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

RUN mkdir -p /data
ENV ConnectionStrings__Database="Data Source=/data/fitness-challenge.db"
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitnessChallenge.Api.dll"]
