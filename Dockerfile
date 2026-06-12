# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivo de projeto e restaurar dependências
COPY ["NutriFlow.csproj", "./"]
RUN dotnet restore "NutriFlow.csproj"

# Copiar apenas os arquivos de código e recursos necessários para compilação (evitando COPY . .)
COPY Components/ ./Components/
COPY Data/ ./Data/
COPY Models/ ./Models/
COPY Repositories/ ./Repositories/
COPY Services/ ./Services/
COPY Properties/ ./Properties/
COPY wwwroot/ ./wwwroot/
COPY Program.cs ./
COPY appsettings.json ./

RUN dotnet build "NutriFlow.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "NutriFlow.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar arquivos sem dar propriedade de escrita ao usuário não-root (deixando-os como somente leitura)
COPY --from=publish /app/publish .

# Definir variáveis de ambiente padrão e expor porta não privilegiada (8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Mudar para o usuário não-root 'app' para execução (que terá apenas permissão de leitura sobre os binários)
USER app

ENTRYPOINT ["dotnet", "NutriFlow.dll"]
