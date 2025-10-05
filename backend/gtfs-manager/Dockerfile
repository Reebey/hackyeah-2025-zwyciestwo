FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# przyspieszenie restore (opcjonalnie)
COPY ./gtfs-manager.csproj ./
RUN dotnet restore

# reszta kodu
COPY . .

# katalog na dane (mount w compose i tak je podmieni)
RUN mkdir -p /app/Data

EXPOSE 5089
CMD ["dotnet","watch","run","--urls","http://0.0.0.0:5089"]
