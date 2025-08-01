FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG VERSION=0.0.0.0
RUN dotnet tool install -g dotnet-setversion
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY ./src/ /src/
WORKDIR /src/Adliance.AzureBlobSimulator
RUN setversion $VERSION
RUN dotnet publish "Adliance.AzureBlobSimulator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
EXPOSE 80
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Adliance.AzureBlobSimulator.dll"]